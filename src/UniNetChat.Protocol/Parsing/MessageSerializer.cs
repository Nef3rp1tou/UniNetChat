using System.Text;
using UniNetChat.Protocol.Messages;

namespace UniNetChat.Protocol.Parsing;

/// <summary>
/// Serializes LNCP messages to bytes/strings.
/// </summary>
public static class MessageSerializer
{
    /// <summary>
    /// Serializes a message to a string.
    /// </summary>
    public static string Serialize(LncpMessage message)
    {
        var sb = new StringBuilder();

        // Status line
        sb.Append(LncpConstants.ProtocolVersion);
        sb.Append(' ');
        sb.Append(message.TypeString);
        sb.Append(LncpConstants.LineEnding);

        // Request-Id header (always present)
        sb.Append(LncpConstants.Headers.RequestId);
        sb.Append(LncpConstants.HeaderSeparator);
        sb.Append(message.RequestId.ToString());
        sb.Append(LncpConstants.LineEnding);

        // Type-specific headers
        WriteTypeSpecificHeaders(sb, message);

        // Custom headers
        foreach (var header in message.Headers)
        {
            sb.Append(header.Key);
            sb.Append(LncpConstants.HeaderSeparator);
            sb.Append(header.Value);
            sb.Append(LncpConstants.LineEnding);
        }

        // Empty line separating headers from body
        sb.Append(LncpConstants.LineEnding);

        // Body
        if (!string.IsNullOrEmpty(message.Body))
        {
            sb.Append(message.Body);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Serializes a message to UTF-8 bytes.
    /// </summary>
    public static byte[] SerializeToBytes(LncpMessage message)
    {
        return Encoding.UTF8.GetBytes(Serialize(message));
    }

    private static void WriteTypeSpecificHeaders(StringBuilder sb, LncpMessage message)
    {
        switch (message)
        {
            case DiscoverMessage discover:
                WriteHeader(sb, LncpConstants.Headers.Nickname, discover.Nickname);
                WriteHeader(sb, LncpConstants.Headers.From, discover.FromNickname);
                WriteHeader(sb, LncpConstants.Headers.Port, discover.Port.ToString());
                WriteHeader(sb, LncpConstants.Headers.Deadline, discover.Deadline.ToString("O"));
                break;

            case ConnectMessage connect:
                WriteHeader(sb, LncpConstants.Headers.Nickname, connect.Nickname);
                break;

            case AcceptMessage accept:
                WriteHeader(sb, LncpConstants.Headers.Nickname, accept.Nickname);
                break;

            case RejectMessage reject:
                if (!string.IsNullOrEmpty(reject.Reason))
                {
                    WriteHeader(sb, LncpConstants.Headers.Reason, reject.Reason);
                }
                break;

            case TextMessage text:
                WriteHeader(sb, LncpConstants.Headers.From, text.From);
                WriteHeader(sb, LncpConstants.Headers.Timestamp, text.Timestamp.ToString("O"));
                WriteHeader(sb, LncpConstants.Headers.Sequence, text.Sequence.ToString());
                break;

            case AckMessage ack:
                WriteHeader(sb, LncpConstants.Headers.Timestamp, ack.Timestamp.ToString("O"));
                WriteHeader(sb, LncpConstants.Headers.Sequence, ack.Sequence.ToString());
                break;

            case CloseMessage close:
                if (!string.IsNullOrEmpty(close.Reason))
                {
                    WriteHeader(sb, LncpConstants.Headers.Reason, close.Reason);
                }
                break;

            case ClosedMessage:
            case HeartbeatMessage:
            case HeartbeatAckMessage:
                // No additional headers
                break;
        }
    }

    private static void WriteHeader(StringBuilder sb, string name, string value)
    {
        sb.Append(name);
        sb.Append(LncpConstants.HeaderSeparator);
        sb.Append(value);
        sb.Append(LncpConstants.LineEnding);
    }
}
