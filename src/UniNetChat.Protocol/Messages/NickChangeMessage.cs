namespace UniNetChat.Protocol.Messages;

/// <summary>
/// Message to notify peer of nickname change.
/// </summary>
public class NickChangeMessage : LncpMessage
{
    public override MessageType Type => MessageType.NickChange;

    /// <summary>
    /// Gets or sets the old nickname.
    /// </summary>
    public string OldNickname { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new nickname.
    /// </summary>
    public string NewNickname { get; set; } = string.Empty;

    public NickChangeMessage()
    {
    }

    public NickChangeMessage(Guid requestId, string oldNickname, string newNickname)
    {
        RequestId = requestId;
        OldNickname = oldNickname;
        NewNickname = newNickname;
    }
}
