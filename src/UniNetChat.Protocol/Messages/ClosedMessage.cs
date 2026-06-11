namespace UniNetChat.Protocol.Messages;

/// <summary>
/// TCP acknowledgment of connection closure.
/// </summary>
public class ClosedMessage : LncpMessage
{
    public override MessageType Type => MessageType.Closed;

    public ClosedMessage()
    {
    }

    public ClosedMessage(Guid requestId)
    {
        RequestId = requestId;
    }
}
