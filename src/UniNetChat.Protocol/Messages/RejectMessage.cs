namespace UniNetChat.Protocol.Messages;

/// <summary>
/// TCP message from initiator rejecting the connection.
/// </summary>
public class RejectMessage : LncpMessage
{
    public override MessageType Type => MessageType.Reject;

    /// <summary>
    /// Gets or sets the reason for rejection.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    public RejectMessage()
    {
    }

    public RejectMessage(Guid requestId, string reason)
    {
        RequestId = requestId;
        Reason = reason;
    }
}
