namespace UniNetChat.Protocol;

/// <summary>
/// Helper class for colored console output.
/// </summary>
public static class ConsoleHelper
{
    private static readonly object _lock = new();

    /// <summary>
    /// Writes a status message in cyan.
    /// </summary>
    public static void WriteStatus(string message)
    {
        WriteColored($"[Status] {message}", ConsoleColor.Cyan);
    }

    /// <summary>
    /// Writes an error message in red.
    /// </summary>
    public static void WriteError(string message)
    {
        WriteColored($"[Error] {message}", ConsoleColor.Red);
    }

    /// <summary>
    /// Writes a success message in green.
    /// </summary>
    public static void WriteSuccess(string message)
    {
        WriteColored($"[OK] {message}", ConsoleColor.Green);
    }

    /// <summary>
    /// Writes a warning message in yellow.
    /// </summary>
    public static void WriteWarning(string message)
    {
        WriteColored($"[Warning] {message}", ConsoleColor.Yellow);
    }

    /// <summary>
    /// Writes an incoming message in magenta.
    /// </summary>
    public static void WriteIncoming(string from, string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"[{from}] ");
            Console.ResetColor();
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Writes an outgoing message in white.
    /// </summary>
    public static void WriteOutgoing(string from, string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{from}] ");
            Console.ResetColor();
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Writes a system message in dark gray.
    /// </summary>
    public static void WriteSystem(string message)
    {
        WriteColored(message, ConsoleColor.DarkGray);
    }

    /// <summary>
    /// Writes a debug message in dark yellow.
    /// </summary>
    public static void WriteDebug(string message)
    {
        WriteColored($"[Debug] {message}", ConsoleColor.DarkYellow);
    }

    /// <summary>
    /// Writes a heartbeat indicator.
    /// </summary>
    public static void WriteHeartbeat(bool sent)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(sent ? "♥" : "♡");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Writes text with the specified color.
    /// </summary>
    public static void WriteColored(string message, ConsoleColor color)
    {
        lock (_lock)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Writes the application header.
    /// </summary>
    public static void WriteHeader(string title)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════╗");
            Console.WriteLine($"║  {title,-40}║");
            Console.WriteLine("╚══════════════════════════════════════════╝");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Writes a separator line.
    /// </summary>
    public static void WriteSeparator()
    {
        WriteColored("────────────────────────────────────────────", ConsoleColor.DarkGray);
    }

    /// <summary>
    /// Prompts the user for input with a colored prompt.
    /// </summary>
    public static string? Prompt(string prompt, ConsoleColor color = ConsoleColor.White)
    {
        lock (_lock)
        {
            Console.ForegroundColor = color;
            Console.Write(prompt);
            Console.ResetColor();
        }
        return Console.ReadLine();
    }
}
