namespace UniNetChat.Protocol.Messages;

/// <summary>
/// Heartbeat message to detect dead connections.
/// Sent periodically to verify the peer is still alive.
/// </summary>
public class HeartbeatMessage : LncpMessage
{
    public override MessageType Type => MessageType.Heartbeat;

    public HeartbeatMessage()
    {
    }

    public HeartbeatMessage(Guid requestId)
    {
        RequestId = requestId;
    }
}
