namespace UniNetChat.Protocol.Messages;

/// <summary>
/// Base class for all LNCP protocol messages.
/// </summary>
public abstract class LncpMessage
{
    /// <summary>
    /// Gets the message type.
    /// </summary>
    public abstract MessageType Type { get; }

    /// <summary>
    /// Gets or sets the request ID (UUID) for this session.
    /// </summary>
    public Guid RequestId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets additional headers for this message.
    /// </summary>
    public Dictionary<string, string> Headers { get; } = new();

    /// <summary>
    /// Gets or sets the message body.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gets the message type as a protocol string.
    /// </summary>
    public string TypeString => Type switch
    {
        MessageType.Discover => "DISCOVER",
        MessageType.Connect => "CONNECT",
        MessageType.Accept => "ACCEPT",
        MessageType.Reject => "REJECT",
        MessageType.Msg => "MSG",
        MessageType.Ack => "ACK",
        MessageType.Close => "CLOSE",
        MessageType.Closed => "CLOSED",
        MessageType.Heartbeat => "HEARTBEAT",
        MessageType.HeartbeatAck => "HEARTBEAT_ACK",
        MessageType.NickChange => "NICK_CHANGE",
        MessageType.NickAck => "NICK_ACK",
        _ => throw new ArgumentOutOfRangeException()
    };

    /// <summary>
    /// Parses a message type string to the enum value.
    /// </summary>
    public static MessageType ParseType(string typeString)
    {
        return typeString.ToUpperInvariant() switch
        {
            "DISCOVER" => MessageType.Discover,
            "CONNECT" => MessageType.Connect,
            "ACCEPT" => MessageType.Accept,
            "REJECT" => MessageType.Reject,
            "MSG" => MessageType.Msg,
            "ACK" => MessageType.Ack,
            "CLOSE" => MessageType.Close,
            "CLOSED" => MessageType.Closed,
            "HEARTBEAT" => MessageType.Heartbeat,
            "HEARTBEAT_ACK" => MessageType.HeartbeatAck,
            "NICK_CHANGE" => MessageType.NickChange,
            "NICK_ACK" => MessageType.NickAck,
            _ => throw new ArgumentException($"Unknown message type: {typeString}")
        };
    }
}
