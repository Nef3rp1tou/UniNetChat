namespace UniNetChat.Protocol;

/// <summary>
/// Explicit reason codes for connection rejection.
/// </summary>
public enum RejectReason
{
    /// <summary>
    /// The UUID in the connection request doesn't match the discovery UUID.
    /// </summary>
    UuidMismatch,

    /// <summary>
    /// The discovery deadline has expired.
    /// </summary>
    DeadlineExpired,

    /// <summary>
    /// The received message was malformed or unparseable.
    /// </summary>
    MalformedMessage,

    /// <summary>
    /// The user manually declined the connection.
    /// </summary>
    UserDeclined,

    /// <summary>
    /// The recipient is already in a chat session.
    /// </summary>
    AlreadyConnected,

    /// <summary>
    /// Unknown or unspecified error.
    /// </summary>
    Unknown
}

/// <summary>
/// Extension methods for RejectReason.
/// </summary>
public static class RejectReasonExtensions
{
    /// <summary>
    /// Converts the reason to a protocol string.
    /// </summary>
    public static string ToProtocolString(this RejectReason reason)
    {
        return reason switch
        {
            RejectReason.UuidMismatch => "uuid_mismatch",
            RejectReason.DeadlineExpired => "deadline_expired",
            RejectReason.MalformedMessage => "malformed_message",
            RejectReason.UserDeclined => "user_declined",
            RejectReason.AlreadyConnected => "already_connected",
            RejectReason.Unknown => "unknown",
            _ => "unknown"
        };
    }

    /// <summary>
    /// Parses a protocol string to a RejectReason.
    /// </summary>
    public static RejectReason ParseRejectReason(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "uuid_mismatch" => RejectReason.UuidMismatch,
            "deadline_expired" => RejectReason.DeadlineExpired,
            "malformed_message" => RejectReason.MalformedMessage,
            "user_declined" => RejectReason.UserDeclined,
            "already_connected" => RejectReason.AlreadyConnected,
            _ => RejectReason.Unknown
        };
    }

    /// <summary>
    /// Gets a human-readable description of the reason.
    /// </summary>
    public static string ToHumanReadable(this RejectReason reason)
    {
        return reason switch
        {
            RejectReason.UuidMismatch => "Request ID does not match",
            RejectReason.DeadlineExpired => "Connection deadline has expired",
            RejectReason.MalformedMessage => "Received malformed message",
            RejectReason.UserDeclined => "User declined the connection",
            RejectReason.AlreadyConnected => "Already in another chat session",
            RejectReason.Unknown => "Unknown error",
            _ => "Unknown error"
        };
    }
}
