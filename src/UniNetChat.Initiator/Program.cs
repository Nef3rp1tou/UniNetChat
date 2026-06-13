using UniNetChat.Initiator;
using UniNetChat.Protocol;

// Parse command line arguments
string nickname = "Initiator";
string? targetNickname = null;
int port = LncpConstants.DefaultTcpPort;
bool enableLogging = true;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--nickname" or "-n":
            if (i + 1 < args.Length) nickname = args[++i];
            break;
        case "--target" or "-t":
            if (i + 1 < args.Length) targetNickname = args[++i];
            break;
        case "--port" or "-p":
            if (i + 1 < args.Length) port = int.Parse(args[++i]);
            break;
        case "--no-log":
            enableLogging = false;
            break;
        case "--help" or "-h":
            PrintHelp();
            return;
    }
}

ConsoleHelper.WriteHeader("Uni-Net Chat (Initiator)");
ConsoleHelper.WriteSystem($"Your nickname: {nickname}");
ConsoleHelper.WriteSeparator();

if (string.IsNullOrEmpty(targetNickname))
{
    targetNickname = ConsoleHelper.Prompt("Enter target nickname to find: ", ConsoleColor.Yellow)?.Trim();

    if (string.IsNullOrEmpty(targetNickname))
    {
        ConsoleHelper.WriteError("Target nickname is required.");
        return;
    }
}

using var client = new InitiatorClient(nickname, port);
ChatLogger? logger = null;

// Set up event handlers
client.StatusChanged += status => ConsoleHelper.WriteStatus(status);
client.MessageReceived += (from, text) =>
{
    ConsoleHelper.WriteIncoming(from, text);
    logger?.LogReceived(from, text);
};
client.Disconnected += () =>
{
    ConsoleHelper.WriteWarning("Disconnected");
    logger?.LogEnd("Connection closed");
};

// Discover and connect
ConsoleHelper.WriteStatus($"Searching for '{targetNickname}'...");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var connected = await client.DiscoverAndConnectAsync(targetNickname, cancellationToken: cts.Token);

if (!connected)
{
    ConsoleHelper.WriteError("Failed to connect. Make sure the recipient is running.");
    return;
}

// Initialize logger after connection
if (enableLogging)
{
    try
    {
        logger = new ChatLogger(Guid.NewGuid(), nickname, client.ConnectedTo);
        logger.LogEvent("Session started");
        ConsoleHelper.WriteSystem($"Chat log: {logger.LogPath}");
    }
    catch
    {
        ConsoleHelper.WriteWarning("Could not create chat log file");
    }
}

ConsoleHelper.WriteSeparator();
ConsoleHelper.WriteSuccess($"Connected to '{client.ConnectedTo}'");
ConsoleHelper.WriteSystem("Type messages and press Enter to send. Type /quit to disconnect.");
ConsoleHelper.WriteSeparator();

// Main chat loop
while (client.IsConnected && !cts.Token.IsCancellationRequested)
{
    try
    {
        var input = ConsoleHelper.Prompt($"[{nickname}] ", ConsoleColor.White);

        if (string.IsNullOrEmpty(input))
        {
            continue;
        }

        if (input.Equals("/quit", StringComparison.OrdinalIgnoreCase))
        {
            logger?.LogEvent("User initiated disconnect");
            await client.CloseAsync("User quit");
            break;
        }

        await client.SendTextAsync(input);
        logger?.LogSent(nickname, input);
    }
    catch (InvalidOperationException)
    {
        break;
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
    Console.WriteLine("Uni-Net Chat Initiator");
    Console.WriteLine();
    Console.WriteLine("Usage: UniNetChat.Initiator [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -n, --nickname <name>   Your nickname (default: Initiator)");
    Console.WriteLine("  -t, --target <name>     Target recipient's nickname");
    Console.WriteLine("  -p, --port <port>       TCP port to listen on (default: 5001)");
    Console.WriteLine("  --no-log                Disable chat logging");
    Console.WriteLine("  -h, --help              Show this help message");
}
