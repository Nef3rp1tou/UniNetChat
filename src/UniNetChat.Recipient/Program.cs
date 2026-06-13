using UniNetChat.Recipient;
using UniNetChat.Protocol;

// Parse command line arguments
string nickname = "Recipient";
bool enableLogging = true;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--nickname" or "-n":
            if (i + 1 < args.Length) nickname = args[++i];
            break;
        case "--no-log":
            enableLogging = false;
            break;
        case "--help" or "-h":
            PrintHelp();
            return;
    }
}

ConsoleHelper.WriteHeader("Uni-Net Chat (Recipient)");
ConsoleHelper.WriteSystem($"Your nickname: {nickname}");
ConsoleHelper.WriteSeparator();

using var server = new RecipientServer(nickname);
using var cts = new CancellationTokenSource();
ChatLogger? logger = null;

// Set up event handlers
server.StatusChanged += status => ConsoleHelper.WriteStatus(status);
server.MessageReceived += (from, text) =>
{
    ConsoleHelper.WriteIncoming(from, text);
    logger?.LogReceived(from, text);
};
server.ConnectionRequest += (from, endpoint) =>
{
    ConsoleHelper.WriteColored($"[Request] Connection from '{from}' at {endpoint}", ConsoleColor.Yellow);
};
server.Disconnected += () =>
{
    ConsoleHelper.WriteWarning("Disconnected");
    logger?.LogEnd("Connection closed");
    logger?.Dispose();
    logger = null;
    ConsoleHelper.WriteStatus("Waiting for new connections...");
};

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// Start listening in background
var listenTask = Task.Run(() => server.StartListeningAsync(cts.Token));

ConsoleHelper.WriteStatus("Waiting for connections...");
ConsoleHelper.WriteSystem("Press Ctrl+C to exit.");
ConsoleHelper.WriteSeparator();

// Main chat loop
while (!cts.Token.IsCancellationRequested)
{
    try
    {
        var input = Console.ReadLine();

        if (cts.Token.IsCancellationRequested)
        {
            break;
        }

        if (string.IsNullOrEmpty(input))
        {
            continue;
        }

        if (input.Equals("/quit", StringComparison.OrdinalIgnoreCase))
        {
            if (server.IsConnected)
            {
                logger?.LogEvent("User initiated disconnect");
                await server.CloseAsync("User quit");
            }
            cts.Cancel();
            break;
        }

        if (!server.IsConnected)
        {
            ConsoleHelper.WriteWarning("Not connected. Waiting for incoming connection...");
            continue;
        }

        // Initialize logger on first message if connected
        if (logger == null && enableLogging && server.IsConnected)
        {
            try
            {
                logger = new ChatLogger(Guid.NewGuid(), nickname, server.ConnectedTo);
                logger.LogEvent("Session started");
                ConsoleHelper.WriteSystem($"Chat log: {logger.LogPath}");
            }
            catch
            {
                ConsoleHelper.WriteWarning("Could not create chat log file");
            }
        }

        ConsoleHelper.WriteOutgoing(nickname, input);
        await server.SendTextAsync(input);
        logger?.LogSent(nickname, input);
    }
    catch (InvalidOperationException)
    {
        // Connection lost
    }
    catch (OperationCanceledException)
    {
        break;
    }
}

logger?.Dispose();
ConsoleHelper.WriteSystem("Goodbye!");

static void PrintHelp()
{
    Console.WriteLine("Uni-Net Chat Recipient");
    Console.WriteLine();
    Console.WriteLine("Usage: UniNetChat.Recipient [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -n, --nickname <name>   Your nickname (default: Recipient)");
    Console.WriteLine("  --no-log                Disable chat logging");
    Console.WriteLine("  -h, --help              Show this help message");
}
