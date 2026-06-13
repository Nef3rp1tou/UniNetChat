namespace UniNetChat.Protocol.Messages;

/// <summary>
/// Acknowledgment of nickname change.
/// </summary>
public class NickAckMessage : LncpMessage
{
    public override MessageType Type => MessageType.NickAck;

    /// <summary>
    /// Gets or sets the acknowledged new nickname.
    /// </summary>
    public string Nickname { get; set; } = string.Empty;

    public NickAckMessage()
    {
    }

    public NickAckMessage(Guid requestId, string nickname)
    {
        RequestId = requestId;
        Nickname = nickname;
    }
}
