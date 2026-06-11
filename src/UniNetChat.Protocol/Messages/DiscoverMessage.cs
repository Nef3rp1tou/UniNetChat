namespace UniNetChat.Protocol.Messages;

/// <summary>
/// UDP broadcast message to discover a recipient on the network.
/// </summary>
public class DiscoverMessage : LncpMessage
{
    public override MessageType Type => MessageType.Discover;

    /// <summary>
    /// Gets or sets the nickname of the target recipient.
    /// </summary>
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the deadline for responses (ISO 8601 timestamp).
    /// </summary>
    public DateTime Deadline { get; set; }

    /// <summary>
    /// Gets or sets the TCP port the initiator is listening on.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the initiator's nickname.
    /// </summary>
    public string FromNickname { get; set; } = string.Empty;

    public DiscoverMessage()
    {
    }

    public DiscoverMessage(string targetNickname, string fromNickname, int port, int timeoutSeconds = LncpConstants.DefaultDiscoveryTimeoutSeconds)
    {
        Nickname = targetNickname;
        FromNickname = fromNickname;
        Port = port;
        Deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
    }
}
