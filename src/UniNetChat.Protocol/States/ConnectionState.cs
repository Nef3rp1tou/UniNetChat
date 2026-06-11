namespace UniNetChat.Protocol.States;

/// <summary>
/// Connection states for the initiator.
/// </summary>
public enum InitiatorState
{
    /// <summary>
    /// Initial state, not connected.
    /// </summary>
    Idle,

    /// <summary>
    /// Broadcasting UDP discovery messages.
    /// </summary>
    Discovering,

    /// <summary>
    /// Waiting for recipient to connect via TCP.
    /// </summary>
    WaitingConnection,

    /// <summary>
    /// TCP connected, performing handshake.
    /// </summary>
    Handshaking,

    /// <summary>
    /// Fully connected, can exchange messages.
    /// </summary>
    Connected,

    /// <summary>
    /// Closing connection, waiting for acknowledgment.
    /// </summary>
    Closing,

    /// <summary>
    /// Connection closed.
    /// </summary>
    Closed
}

/// <summary>
/// Connection states for the recipient.
/// </summary>
public enum RecipientState
{
    /// <summary>
    /// Listening for UDP discovery broadcasts.
    /// </summary>
    Listening,

    /// <summary>
    /// Connecting to initiator via TCP.
    /// </summary>
    Connecting,

    /// <summary>
    /// TCP connected, performing handshake.
    /// </summary>
    Handshaking,

    /// <summary>
    /// Fully connected, can exchange messages.
    /// </summary>
    Connected,

    /// <summary>
    /// Closing connection, waiting for acknowledgment.
    /// </summary>
    Closing,

    /// <summary>
    /// Connection closed.
    /// </summary>
    Closed
}
