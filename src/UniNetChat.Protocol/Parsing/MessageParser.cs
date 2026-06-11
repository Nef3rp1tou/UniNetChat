using System.Text;
using UniNetChat.Protocol.Messages;

namespace UniNetChat.Protocol.Parsing;

/// <summary>
/// Parses LNCP messages from bytes/strings.
/// </summary>
public static class MessageParser
{
    /// <summary>
    /// Parses a message from a string.
    /// </summary>
    public static LncpMessage Parse(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            throw new FormatException("Empty message data");
        }

        var lines = data.Split(new[] { "\r\n" }, StringSplitOptions.None);
        if (lines.Length < 2)
        {
            throw new FormatException("Message too short");
        }

        // Parse status line: "LNCP/1.0 MESSAGE_TYPE"
        var statusLine = lines[0];
        var statusParts = statusLine.Split(' ', 2);
        if (statusParts.Length != 2)
        {
            throw new FormatException($"Invalid status line: {statusLine}");
        }

        if (statusParts[0] != LncpConstants.ProtocolVersion)
        {
            throw new FormatException($"Unsupported protocol version: {statusParts[0]}");
        }

        var messageType = LncpMessage.ParseType(statusParts[1]);

        // Parse headers until empty line
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int bodyStartIndex = -1;

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrEmpty(line))
            {
                bodyStartIndex = i + 1;
                break;
            }

            var colonIndex = line.IndexOf(':');
            if (colonIndex <= 0)
            {
                throw new FormatException($"Invalid header line: {line}");
            }

            var headerName = line.Substring(0, colonIndex).Trim();
            var headerValue = line.Substring(colonIndex + 1).TrimStart();
            headers[headerName] = headerValue;
        }

        // Parse body (everything after the empty line)
        string body = string.Empty;
        if (bodyStartIndex > 0 && bodyStartIndex < lines.Length)
        {
            body = string.Join("\r\n", lines.Skip(bodyStartIndex));
        }

        // Get Request-Id
        if (!headers.TryGetValue(LncpConstants.Headers.RequestId, out var requestIdStr) ||
            !Guid.TryParse(requestIdStr, out var requestId))
        {
            throw new FormatException("Missing or invalid Request-Id header");
        }

        // Create the appropriate message type
        var message = CreateMessage(messageType, requestId, headers, body);

        // Copy any additional headers
        foreach (var header in headers)
        {
            if (!IsKnownHeader(header.Key))
            {
                message.Headers[header.Key] = header.Value;
            }
        }

        return message;
    }

    /// <summary>
    /// Parses a message from UTF-8 bytes.
    /// </summary>
    public static LncpMessage Parse(byte[] data)
    {
        return Parse(Encoding.UTF8.GetString(data));
    }

    /// <summary>
    /// Tries to parse a message, returning null if parsing fails.
    /// </summary>
    public static LncpMessage? TryParse(string data, out string? error)
    {
        try
        {
            error = null;
            return Parse(data);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }

    /// <summary>
    /// Tries to parse a message from bytes, returning null if parsing fails.
    /// </summary>
    public static LncpMessage? TryParse(byte[] data, out string? error)
    {
        return TryParse(Encoding.UTF8.GetString(data), out error);
    }

    private static LncpMessage CreateMessage(MessageType type, Guid requestId,
        Dictionary<string, string> headers, string body)
    {
        return type switch
        {
            MessageType.Discover => CreateDiscoverMessage(requestId, headers),
            MessageType.Connect => CreateConnectMessage(requestId, headers),
            MessageType.Accept => CreateAcceptMessage(requestId, headers),
            MessageType.Reject => CreateRejectMessage(requestId, headers),
            MessageType.Msg => CreateTextMessage(requestId, headers, body),
            MessageType.Ack => CreateAckMessage(requestId, headers),
            MessageType.Close => CreateCloseMessage(requestId, headers),
            MessageType.Closed => new ClosedMessage(requestId),
            _ => throw new FormatException($"Unknown message type: {type}")
        };
    }

    private static DiscoverMessage CreateDiscoverMessage(Guid requestId, Dictionary<string, string> headers)
    {
        var message = new DiscoverMessage
        {
            RequestId = requestId,
            Nickname = GetRequiredHeader(headers, LncpConstants.Headers.Nickname),
            FromNickname = GetRequiredHeader(headers, LncpConstants.Headers.From),
            Port = int.Parse(GetRequiredHeader(headers, LncpConstants.Headers.Port)),
            Deadline = DateTime.Parse(GetRequiredHeader(headers, LncpConstants.Headers.Deadline))
        };
        return message;
    }

    private static ConnectMessage CreateConnectMessage(Guid requestId, Dictionary<string, string> headers)
    {
        return new ConnectMessage(requestId, GetRequiredHeader(headers, LncpConstants.Headers.Nickname));
    }

    private static AcceptMessage CreateAcceptMessage(Guid requestId, Dictionary<string, string> headers)
    {
        return new AcceptMessage(requestId, GetRequiredHeader(headers, LncpConstants.Headers.Nickname));
    }

    private static RejectMessage CreateRejectMessage(Guid requestId, Dictionary<string, string> headers)
    {
        headers.TryGetValue(LncpConstants.Headers.Reason, out var reason);
        return new RejectMessage(requestId, reason ?? string.Empty);
    }

    private static TextMessage CreateTextMessage(Guid requestId, Dictionary<string, string> headers, string body)
    {
        return new TextMessage
        {
            RequestId = requestId,
            From = GetRequiredHeader(headers, LncpConstants.Headers.From),
            Timestamp = DateTime.Parse(GetRequiredHeader(headers, LncpConstants.Headers.Timestamp)),
            Sequence = int.Parse(GetRequiredHeader(headers, LncpConstants.Headers.Sequence)),
            Text = body
        };
    }

    private static AckMessage CreateAckMessage(Guid requestId, Dictionary<string, string> headers)
    {
        return new AckMessage
        {
            RequestId = requestId,
            Timestamp = DateTime.Parse(GetRequiredHeader(headers, LncpConstants.Headers.Timestamp)),
            Sequence = int.Parse(GetRequiredHeader(headers, LncpConstants.Headers.Sequence))
        };
    }

    private static CloseMessage CreateCloseMessage(Guid requestId, Dictionary<string, string> headers)
    {
        headers.TryGetValue(LncpConstants.Headers.Reason, out var reason);
        return new CloseMessage(requestId, reason ?? string.Empty);
    }

    private static string GetRequiredHeader(Dictionary<string, string> headers, string name)
    {
        if (!headers.TryGetValue(name, out var value))
        {
            throw new FormatException($"Missing required header: {name}");
        }
        return value;
    }

    private static bool IsKnownHeader(string name)
    {
        return name.Equals(LncpConstants.Headers.RequestId, StringComparison.OrdinalIgnoreCase) ||
               name.Equals(LncpConstants.Headers.Nickname, StringComparison.OrdinalIgnoreCase) ||
               name.Equals(LncpConstants.Headers.Deadline, StringComparison.OrdinalIgnoreCase) ||
               name.Equals(LncpConstants.Headers.Port, StringComparison.OrdinalIgnoreCase) ||
               name.Equals(LncpConstants.Headers.From, StringComparison.OrdinalIgnoreCase) ||
               name.Equals(LncpConstants.Headers.Timestamp, StringComparison.OrdinalIgnoreCase) ||
               name.Equals(LncpConstants.Headers.Sequence, StringComparison.OrdinalIgnoreCase) ||
               name.Equals(LncpConstants.Headers.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
