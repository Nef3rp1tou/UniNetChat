namespace UniNetChat.Protocol.Messages;

/// <summary>
/// TCP message from initiator rejecting the connection.
/// </summary>
public class RejectMessage : LncpMessage
{
    public override MessageType Type => MessageType.Reject;

    /// <summary>
    /// Gets or sets the machine-readable reason code.
    /// </summary>
    public RejectReason ReasonCode { get; set; } = RejectReason.Unknown;

    /// <summary>
    /// Gets or sets additional human-readable reason text.
    /// </summary>
    public string ReasonText { get; set; } = string.Empty;

    /// <summary>
    /// Gets the combined reason string for protocol transmission.
    /// </summary>
    public string Reason => string.IsNullOrEmpty(ReasonText)
        ? ReasonCode.ToProtocolString()
        : $"{ReasonCode.ToProtocolString()}:{ReasonText}";

    public RejectMessage()
    {
    }

    public RejectMessage(Guid requestId, RejectReason reasonCode, string? reasonText = null)
    {
        RequestId = requestId;
        ReasonCode = reasonCode;
        ReasonText = reasonText ?? string.Empty;
    }

    /// <summary>
    /// Parses a reason string into code and text components.
    /// </summary>
    public static (RejectReason Code, string Text) ParseReason(string reason)
    {
        if (string.IsNullOrEmpty(reason))
        {
            return (RejectReason.Unknown, string.Empty);
        }

        var colonIndex = reason.IndexOf(':');
        if (colonIndex < 0)
        {
            return (RejectReasonExtensions.ParseRejectReason(reason), string.Empty);
        }

        var codeStr = reason.Substring(0, colonIndex);
        var text = reason.Substring(colonIndex + 1);
        return (RejectReasonExtensions.ParseRejectReason(codeStr), text);
    }
}
