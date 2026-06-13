namespace UniNetChat.Protocol.Messages;

/// <summary>
/// Defines the types of messages in the LNCP protocol.
/// </summary>
public enum MessageType
{
    /// <summary>
    /// UDP broadcast to find a recipient on the network.
    /// </summary>
    Discover,

    /// <summary>
    /// TCP message from recipient to initiator to establish connection.
    /// </summary>
    Connect,

    /// <summary>
    /// TCP message from initiator accepting the connection.
    /// </summary>
    Accept,

    /// <summary>
    /// TCP message from initiator rejecting the connection.
    /// </summary>
    Reject,

    /// <summary>
    /// TCP message containing chat text (bidirectional).
    /// </summary>
    Msg,

    /// <summary>
    /// TCP acknowledgment of a received message (bidirectional).
    /// </summary>
    Ack,

    /// <summary>
    /// TCP request to close the connection (bidirectional).
    /// </summary>
    Close,

    /// <summary>
    /// TCP acknowledgment of connection closure (bidirectional).
    /// </summary>
    Closed,

    /// <summary>
    /// TCP heartbeat to verify connection is alive.
    /// </summary>
    Heartbeat,

    /// <summary>
    /// TCP acknowledgment of heartbeat.
    /// </summary>
    HeartbeatAck,

    /// <summary>
    /// TCP notification of nickname change.
    /// </summary>
    NickChange,

    /// <summary>
    /// TCP acknowledgment of nickname change.
    /// </summary>
    NickAck
}
