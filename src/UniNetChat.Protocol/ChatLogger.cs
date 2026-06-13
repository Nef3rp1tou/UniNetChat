using System.Text;

namespace UniNetChat.Protocol;

/// <summary>
/// Logs chat messages to a file for history.
/// </summary>
public class ChatLogger : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly string _logPath;
    private bool _disposed;

    /// <summary>
    /// Gets the path to the log file.
    /// </summary>
    public string LogPath => _logPath;

    /// <summary>
    /// Creates a new chat logger.
    /// </summary>
    /// <param name="sessionId">The session ID (used in filename)</param>
    /// <param name="localNickname">Local user's nickname</param>
    /// <param name="remoteNickname">Remote user's nickname</param>
    public ChatLogger(Guid sessionId, string localNickname, string remoteNickname)
    {
        var logsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniNetChat", "logs");
        Directory.CreateDirectory(logsDir);

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var filename = $"chat_{localNickname}_{remoteNickname}_{timestamp}.log";
        _logPath = Path.Combine(logsDir, filename);

        _writer = new StreamWriter(_logPath, append: false, encoding: Encoding.UTF8);
        _writer.AutoFlush = true;

        WriteHeader(sessionId, localNickname, remoteNickname);
    }

    private void WriteHeader(Guid sessionId, string localNickname, string remoteNickname)
    {
        _writer.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        _writer.WriteLine($"║  LNCP Chat Log                                               ║");
        _writer.WriteLine($"║  Session: {sessionId,-52}║");
        _writer.WriteLine($"║  Local: {localNickname,-54}║");
        _writer.WriteLine($"║  Remote: {remoteNickname,-53}║");
        _writer.WriteLine($"║  Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss,-51}║");
        _writer.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        _writer.WriteLine();
    }

    /// <summary>
    /// Logs a sent message.
    /// </summary>
    public void LogSent(string from, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _writer.WriteLine($"[{timestamp}] {from} → {message}");
    }

    /// <summary>
    /// Logs a received message.
    /// </summary>
    public void LogReceived(string from, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _writer.WriteLine($"[{timestamp}] {from} ← {message}");
    }

    /// <summary>
    /// Logs a system event.
    /// </summary>
    public void LogEvent(string eventDescription)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _writer.WriteLine($"[{timestamp}] *** {eventDescription}");
    }

    /// <summary>
    /// Logs session end.
    /// </summary>
    public void LogEnd(string reason)
    {
        _writer.WriteLine();
        _writer.WriteLine("────────────────────────────────────────────────────────────────");
        _writer.WriteLine($"Session ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _writer.WriteLine($"Reason: {reason}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _writer.Dispose();
        GC.SuppressFinalize(this);
    }
}
