using System.Net;
using System.Net.Sockets;
using UniNetChat.Protocol;
using UniNetChat.Protocol.Messages;
using UniNetChat.Protocol.Parsing;
using UniNetChat.Protocol.States;

namespace UniNetChat.Recipient;

/// <summary>
/// Server that receives chat connection requests.
/// </summary>
public class RecipientServer : IDisposable
{
    private readonly string _nickname;
    private RecipientState _state = RecipientState.Listening;
    private UdpClient? _udpListener;
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private Guid _currentRequestId;
    private string _connectedNickname = string.Empty;
    private int _sequenceNumber = 0;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public RecipientState State => _state;
    public string Nickname => _nickname;
    public string ConnectedTo => _connectedNickname;
    public bool IsConnected => _state == RecipientState.Connected;

    public event Action<string, string>? MessageReceived;
    public event Action<string>? StatusChanged;
    public event Action<string, IPEndPoint>? ConnectionRequest;
    public event Action? Disconnected;

    public RecipientServer(string nickname)
    {
        _nickname = nickname;
    }

    /// <summary>
    /// Starts listening for discovery broadcasts.
    /// </summary>
    public async Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _udpListener = new UdpClient(LncpConstants.DefaultDiscoveryPort);
        TransitionTo(RecipientState.Listening);
        OnStatusChanged($"Listening for discovery on port {LncpConstants.DefaultDiscoveryPort}...");

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpListener.ReceiveAsync(_cts.Token);
                    await HandleDiscoveryAsync(result.Buffer, result.RemoteEndPoint);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    OnStatusChanged($"Error receiving: {ex.Message}");
                }
            }
        }
        finally
        {
            _udpListener?.Dispose();
            _udpListener = null;
        }
    }

    private async Task HandleDiscoveryAsync(byte[] data, IPEndPoint remoteEndpoint)
    {
        var message = MessageParser.TryParse(data, out var error);

        if (message is not DiscoverMessage discoverMessage)
        {
            return;
        }

        // Check if this message is for us
        if (!discoverMessage.Nickname.Equals(_nickname, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Check deadline
        if (DateTime.UtcNow > discoverMessage.Deadline)
        {
            OnStatusChanged($"Received expired discovery from {discoverMessage.FromNickname}");
            return;
        }

        OnStatusChanged($"Discovery from '{discoverMessage.FromNickname}' at {remoteEndpoint.Address}");
        ConnectionRequest?.Invoke(discoverMessage.FromNickname, remoteEndpoint);

        // Connect to initiator
        await ConnectToInitiatorAsync(discoverMessage, remoteEndpoint.Address);
    }

    private async Task ConnectToInitiatorAsync(DiscoverMessage discoverMessage, IPAddress initiatorAddress)
    {
        if (_state != RecipientState.Listening)
        {
            OnStatusChanged("Already in a connection, ignoring discovery");
            return;
        }

        TransitionTo(RecipientState.Connecting);
        _currentRequestId = discoverMessage.RequestId;

        try
        {
            // Connect to initiator's TCP port
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(initiatorAddress, discoverMessage.Port);
            _stream = _tcpClient.GetStream();

            OnStatusChanged($"Connected to {initiatorAddress}:{discoverMessage.Port}");

            TransitionTo(RecipientState.Handshaking);

            // Send CONNECT message
            var connectMessage = new ConnectMessage(_currentRequestId, _nickname);
            await SendMessageAsync(connectMessage);

            // Wait for ACCEPT or REJECT
            var response = await ReadMessageAsync(_cts!.Token);

            if (response is AcceptMessage acceptMessage)
            {
                _connectedNickname = acceptMessage.Nickname;
                TransitionTo(RecipientState.Connected);
                OnStatusChanged($"Handshake complete with '{_connectedNickname}'");

                // Start receiving messages
                _ = ReceiveMessagesAsync(_cts.Token);
            }
            else if (response is RejectMessage rejectMessage)
            {
                OnStatusChanged($"Connection rejected: {rejectMessage.Reason}");
                await CleanupAsync();
            }
            else
            {
                OnStatusChanged($"Unexpected response: {response.GetType().Name}");
                await CleanupAsync();
            }
        }
        catch (Exception ex)
        {
            OnStatusChanged($"Connection failed: {ex.Message}");
            await CleanupAsync();
        }
    }

    /// <summary>
    /// Sends a text message to the connected initiator.
    /// </summary>
    public async Task SendTextAsync(string text)
    {
        if (_state != RecipientState.Connected)
        {
            throw new InvalidOperationException("Not connected");
        }

        var message = new TextMessage(_currentRequestId, _nickname, text, ++_sequenceNumber);
        await SendMessageAsync(message);
    }

    /// <summary>
    /// Closes the connection gracefully.
    /// </summary>
    public async Task CloseAsync(string reason = "User requested disconnect")
    {
        if (_state != RecipientState.Connected)
        {
            return;
        }

        try
        {
            TransitionTo(RecipientState.Closing);
            var closeMessage = new CloseMessage(_currentRequestId, reason);
            await SendMessageAsync(closeMessage);

            // Wait for CLOSED response with timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                await ReadMessageAsync<ClosedMessage>(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Timeout waiting for CLOSED, proceed anyway
            }
        }
        finally
        {
            await CleanupAsync();
        }
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _state == RecipientState.Connected)
            {
                var message = await ReadMessageAsync(cancellationToken);

                switch (message)
                {
                    case TextMessage textMessage:
                        // Send ACK
                        var ack = new AckMessage(_currentRequestId, textMessage.Sequence);
                        await SendMessageAsync(ack);
                        MessageReceived?.Invoke(textMessage.From, textMessage.Text);
                        break;

                    case CloseMessage:
                        var closed = new ClosedMessage(_currentRequestId);
                        await SendMessageAsync(closed);
                        await CleanupAsync();
                        return;

                    case AckMessage:
                        // Message acknowledged
                        break;
                }
            }
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested || _state == RecipientState.Closed)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            OnStatusChanged($"Connection error: {ex.Message}");
            await CleanupAsync();
        }
    }

    private async Task SendMessageAsync(LncpMessage message)
    {
        if (_stream == null)
        {
            throw new InvalidOperationException("Not connected");
        }

        var data = MessageSerializer.SerializeToBytes(message);
        var lengthPrefix = BitConverter.GetBytes(data.Length);

        await _stream.WriteAsync(lengthPrefix);
        await _stream.WriteAsync(data);
        await _stream.FlushAsync();
    }

    private async Task<T> ReadMessageAsync<T>(CancellationToken cancellationToken) where T : LncpMessage
    {
        var message = await ReadMessageAsync(cancellationToken);
        if (message is not T typedMessage)
        {
            throw new InvalidOperationException($"Expected {typeof(T).Name} but received {message.GetType().Name}");
        }
        return typedMessage;
    }

    private async Task<LncpMessage> ReadMessageAsync(CancellationToken cancellationToken)
    {
        if (_stream == null)
        {
            throw new InvalidOperationException("Not connected");
        }

        // Read length prefix (4 bytes)
        var lengthBuffer = new byte[4];
        await _stream.ReadExactlyAsync(lengthBuffer, cancellationToken);
        var length = BitConverter.ToInt32(lengthBuffer, 0);

        if (length <= 0 || length > LncpConstants.MaxMessageSize)
        {
            throw new InvalidOperationException($"Invalid message length: {length}");
        }

        // Read message data
        var buffer = new byte[length];
        await _stream.ReadExactlyAsync(buffer, cancellationToken);

        return MessageParser.Parse(buffer);
    }

    private async Task CleanupAsync()
    {
        TransitionTo(RecipientState.Closed);

        if (_stream != null)
        {
            await _stream.DisposeAsync();
            _stream = null;
        }

        _tcpClient?.Dispose();
        _tcpClient = null;

        _connectedNickname = string.Empty;
        _sequenceNumber = 0;

        Disconnected?.Invoke();

        // Go back to listening state
        TransitionTo(RecipientState.Listening);
    }

    private void TransitionTo(RecipientState newState)
    {
        if (_state != newState)
        {
            _state = newState;
        }
    }

    private void OnStatusChanged(string status)
    {
        StatusChanged?.Invoke(status);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts?.Cancel();
        _cts?.Dispose();
        _stream?.Dispose();
        _tcpClient?.Dispose();
        _udpListener?.Dispose();

        GC.SuppressFinalize(this);
    }
}
