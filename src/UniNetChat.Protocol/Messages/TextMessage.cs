namespace UniNetChat.Protocol.Messages;

/// <summary>
/// TCP message containing chat text.
/// </summary>
public class TextMessage : LncpMessage
{
    public override MessageType Type => MessageType.Msg;

    /// <summary>
    /// Gets or sets the sender's nickname.
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the message sequence number.
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Gets or sets the text content of the message.
    /// </summary>
    public string Text
    {
        get => Body;
        set => Body = value;
    }

    public TextMessage()
    {
        Timestamp = DateTime.UtcNow;
    }

    public TextMessage(Guid requestId, string from, string text, int sequence)
    {
        RequestId = requestId;
        From = from;
        Text = text;
        Sequence = sequence;
        Timestamp = DateTime.UtcNow;
    }
}
