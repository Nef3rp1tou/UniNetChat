namespace UniNetChat.Protocol.Messages;

/// <summary>
/// TCP message from recipient to initiator to establish connection.
/// </summary>
public class ConnectMessage : LncpMessage
{
    public override MessageType Type => MessageType.Connect;

    /// <summary>
    /// Gets or sets the recipient's nickname.
    /// </summary>
    public string Nickname { get; set; } = string.Empty;

    public ConnectMessage()
    {
    }

    public ConnectMessage(Guid requestId, string nickname)
    {
        RequestId = requestId;
        Nickname = nickname;
    }
}
