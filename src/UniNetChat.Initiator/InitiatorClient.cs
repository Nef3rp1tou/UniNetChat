using System.Net;
using System.Net.Sockets;
using System.Text;
using UniNetChat.Protocol;
using UniNetChat.Protocol.Messages;
using UniNetChat.Protocol.Parsing;
using UniNetChat.Protocol.States;

namespace UniNetChat.Initiator;

/// <summary>
/// Client that initiates chat connections.
/// </summary>
public class InitiatorClient : IDisposable
{
    private readonly string _nickname;
    private readonly int _tcpPort;
    private InitiatorState _state = InitiatorState.Idle;
    private TcpListener? _tcpListener;
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private Guid _currentRequestId;
    private string _connectedNickname = string.Empty;
    private int _sequenceNumber = 0;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public InitiatorState State => _state;
    public string ConnectedTo => _connectedNickname;
    public bool IsConnected => _state == InitiatorState.Connected;

    public event Action<string, string>? MessageReceived;
    public event Action<string>? StatusChanged;
    public event Action? Disconnected;
    public event Action<string, string>? NicknameChanged;

    public InitiatorClient(string nickname, int tcpPort = LncpConstants.DefaultTcpPort)
    {
        _nickname = nickname;
        _tcpPort = tcpPort;
    }

    /// <summary>
    /// Discovers and connects to a recipient with the given nickname.
    /// </summary>
    public async Task<bool> DiscoverAndConnectAsync(string targetNickname, int timeoutSeconds = LncpConstants.DefaultDiscoveryTimeoutSeconds, CancellationToken cancellationToken = default)
    {
        if (_state != InitiatorState.Idle)
        {
            throw new InvalidOperationException($"Cannot discover in state {_state}");
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _currentRequestId = Guid.NewGuid();

        try
        {
            // Start TCP listener first
            _tcpListener = new TcpListener(IPAddress.Any, _tcpPort);
            _tcpListener.Start();
            TransitionTo(InitiatorState.WaitingConnection);
            OnStatusChanged($"Listening on port {_tcpPort}");

            // Broadcast discovery message
            TransitionTo(InitiatorState.Discovering);
            var discoverMessage = new DiscoverMessage(targetNickname, _nickname, _tcpPort, timeoutSeconds);
            discoverMessage.RequestId = _currentRequestId;

            await BroadcastDiscoveryAsync(discoverMessage);
            OnStatusChanged($"Searching for '{targetNickname}'...");

            TransitionTo(InitiatorState.WaitingConnection);

            // Wait for TCP connection with timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeoutCts.Token);

            try
            {
                _tcpClient = await _tcpListener.AcceptTcpClientAsync(linkedCts.Token);
                _stream = _tcpClient.GetStream();
                OnStatusChanged("Recipient connected, performing handshake...");

                TransitionTo(InitiatorState.Handshaking);

                // Read CONNECT message
                var connectMessage = await ReadMessageAsync<ConnectMessage>(linkedCts.Token);

                if (connectMessage.RequestId != _currentRequestId)
                {
                    await SendRejectAsync(RejectReason.UuidMismatch);
                    return false;
                }

                // Send ACCEPT
                _connectedNickname = connectMessage.Nickname;
                var acceptMessage = new AcceptMessage(_currentRequestId, _nickname);
                await SendMessageAsync(acceptMessage);

                TransitionTo(InitiatorState.Connected);
                OnStatusChanged($"Connected to '{_connectedNickname}'");

                // Start receiving messages in background
                _ = ReceiveMessagesAsync(_cts.Token);

                return true;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                OnStatusChanged("Discovery timed out - no response received");
                await CleanupAsync();
                return false;
            }
        }
        catch (Exception ex)
        {
            OnStatusChanged($"Error: {ex.Message}");
            await CleanupAsync();
            return false;
        }
    }

    /// <summary>
    /// Sends a text message to the connected recipient.
    /// </summary>
    public async Task SendTextAsync(string text)
    {
        if (_state != InitiatorState.Connected)
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
        if (_state != InitiatorState.Connected)
        {
            return;
        }

        try
        {
            TransitionTo(InitiatorState.Closing);
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

    /// <summary>
    /// Changes the local nickname and notifies the peer.
    /// </summary>
    public async Task ChangeNicknameAsync(string oldNickname, string newNickname)
    {
        if (_state != InitiatorState.Connected)
        {
            throw new InvalidOperationException("Not connected");
        }

        var message = new NickChangeMessage(_currentRequestId, oldNickname, newNickname);
        await SendMessageAsync(message);
    }

    private async Task BroadcastDiscoveryAsync(DiscoverMessage message)
    {
        using var udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;

        var data = MessageSerializer.SerializeToBytes(message);
        var endpoint = new IPEndPoint(IPAddress.Broadcast, LncpConstants.DefaultDiscoveryPort);

        // Send multiple times for reliability
        for (int i = 0; i < 3; i++)
        {
            await udpClient.SendAsync(data, data.Length, endpoint);
            await Task.Delay(100);
        }
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _state == InitiatorState.Connected)
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

                    case NickChangeMessage nickChange:
                        // Peer changed their nickname
                        _connectedNickname = nickChange.NewNickname;
                        var nickAck = new NickAckMessage(_currentRequestId, nickChange.NewNickname);
                        await SendMessageAsync(nickAck);
                        NicknameChanged?.Invoke(nickChange.OldNickname, nickChange.NewNickname);
                        break;

                    case NickAckMessage:
                        // Our nickname change was acknowledged
                        break;

                    case HeartbeatMessage:
                        var heartbeatAck = new HeartbeatAckMessage(_currentRequestId);
                        await SendMessageAsync(heartbeatAck);
                        break;

                    case HeartbeatAckMessage:
                        // Heartbeat acknowledged
                        break;
                }
            }
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested || _state == InitiatorState.Closed)
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

    private async Task SendRejectAsync(RejectReason reason, string? details = null)
    {
        var reject = new RejectMessage(_currentRequestId, reason, details);
        await SendMessageAsync(reject);
        await CleanupAsync();
    }

    private async Task CleanupAsync()
    {
        TransitionTo(InitiatorState.Closed);

        _cts?.Cancel();

        if (_stream != null)
        {
            await _stream.DisposeAsync();
            _stream = null;
        }

        _tcpClient?.Dispose();
        _tcpClient = null;

        _tcpListener?.Stop();
        _tcpListener = null;

        _connectedNickname = string.Empty;
        _sequenceNumber = 0;

        Disconnected?.Invoke();

        // Reset to idle for potential reconnection
        _state = InitiatorState.Idle;
    }

    private void TransitionTo(InitiatorState newState)
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
        _tcpListener?.Stop();

        GC.SuppressFinalize(this);
    }
}
