namespace UniNetChat.Protocol.Messages;

/// <summary>
/// TCP message from initiator accepting the connection.
/// </summary>
public class AcceptMessage : LncpMessage
{
    public override MessageType Type => MessageType.Accept;

    /// <summary>
    /// Gets or sets the initiator's nickname.
    /// </summary>
    public string Nickname { get; set; } = string.Empty;

    public AcceptMessage()
    {
    }

    public AcceptMessage(Guid requestId, string nickname)
    {
        RequestId = requestId;
        Nickname = nickname;
    }
}
