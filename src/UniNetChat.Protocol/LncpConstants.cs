namespace UniNetChat.Protocol;

/// <summary>
/// Constants for the LNCP protocol.
/// </summary>
public static class LncpConstants
{
    /// <summary>
    /// Protocol version string.
    /// </summary>
    public const string ProtocolVersion = "LNCP/1.0";

    /// <summary>
    /// Default UDP discovery port.
    /// </summary>
    public const int DefaultDiscoveryPort = 5000;

    /// <summary>
    /// Default TCP communication port.
    /// </summary>
    public const int DefaultTcpPort = 5001;

    /// <summary>
    /// Broadcast address for UDP discovery.
    /// </summary>
    public const string BroadcastAddress = "255.255.255.255";

    /// <summary>
    /// Line ending for protocol messages.
    /// </summary>
    public const string LineEnding = "\r\n";

    /// <summary>
    /// Header separator (colon and space).
    /// </summary>
    public const string HeaderSeparator = ": ";

    /// <summary>
    /// Default discovery timeout in seconds.
    /// </summary>
    public const int DefaultDiscoveryTimeoutSeconds = 30;

    /// <summary>
    /// Default network operation timeout in milliseconds.
    /// </summary>
    public const int DefaultTimeoutMs = 30000;

    /// <summary>
    /// Maximum message size in bytes.
    /// </summary>
    public const int MaxMessageSize = 65536;

    // Header names
    public static class Headers
    {
        public const string RequestId = "Request-Id";
        public const string Nickname = "Nickname";
        public const string Deadline = "Deadline";
        public const string Port = "Port";
        public const string From = "From";
        public const string Timestamp = "Timestamp";
        public const string Sequence = "Sequence";
        public const string Reason = "Reason";
    }
}
