namespace UniNetChat.Protocol.Messages;

/// <summary>
/// Acknowledgment of a heartbeat message.
/// </summary>
public class HeartbeatAckMessage : LncpMessage
{
    public override MessageType Type => MessageType.HeartbeatAck;

    public HeartbeatAckMessage()
    {
    }

    public HeartbeatAckMessage(Guid requestId)
    {
        RequestId = requestId;
    }
}
