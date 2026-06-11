namespace UniNetChat.Protocol.Messages;

/// <summary>
/// TCP request to close the connection.
/// </summary>
public class CloseMessage : LncpMessage
{
    public override MessageType Type => MessageType.Close;

    /// <summary>
    /// Gets or sets the reason for closing.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    public CloseMessage()
    {
    }

    public CloseMessage(Guid requestId, string reason = "")
    {
        RequestId = requestId;
        Reason = reason;
    }
}
