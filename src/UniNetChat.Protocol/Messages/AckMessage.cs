namespace UniNetChat.Protocol.Messages;

/// <summary>
/// TCP acknowledgment of a received message.
/// </summary>
public class AckMessage : LncpMessage
{
    public override MessageType Type => MessageType.Ack;

    /// <summary>
    /// Gets or sets the timestamp of the acknowledgment.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the sequence number being acknowledged.
    /// </summary>
    public int Sequence { get; set; }

    public AckMessage()
    {
        Timestamp = DateTime.UtcNow;
    }

    public AckMessage(Guid requestId, int sequence)
    {
        RequestId = requestId;
        Sequence = sequence;
        Timestamp = DateTime.UtcNow;
    }
}
