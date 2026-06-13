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

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// Discovery loop - allows retry on failure
InitiatorClient? client = null;
ChatLogger? logger = null;
bool connected = false;

while (!connected && !cts.Token.IsCancellationRequested)
{
    // Get target nickname if not provided
    if (string.IsNullOrEmpty(targetNickname))
    {
        targetNickname = ConsoleHelper.Prompt("Enter target nickname to find (or /quit to exit): ", ConsoleColor.Yellow)?.Trim();

        if (string.IsNullOrEmpty(targetNickname))
        {
            continue;
        }

        if (targetNickname.Equals("/quit", StringComparison.OrdinalIgnoreCase))
        {
            ConsoleHelper.WriteSystem("Goodbye!");
            return;
        }
    }

    // Create client and set up handlers
    client?.Dispose();
    client = new InitiatorClient(nickname, port);

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
    client.NicknameChanged += (oldNick, newNick) =>
    {
        ConsoleHelper.WriteStatus($"Peer changed nickname: {oldNick} -> {newNick}");
        logger?.LogEvent($"Peer renamed: {oldNick} -> {newNick}");
    };

    // Try to connect
    ConsoleHelper.WriteStatus($"Searching for '{targetNickname}'...");
    connected = await client.DiscoverAndConnectAsync(targetNickname, cancellationToken: cts.Token);

    if (!connected)
    {
        ConsoleHelper.WriteError($"Could not find '{targetNickname}'. Make sure the recipient is running.");
        ConsoleHelper.WriteSeparator();
        targetNickname = null; // Allow user to enter a new target
    }
}

if (!connected || client == null)
{
    ConsoleHelper.WriteSystem("Goodbye!");
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
ConsoleHelper.WriteSystem("Commands: /nick <name>, /help, /quit");
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

        // Handle commands
        if (input.StartsWith("/"))
        {
            var parts = input.Split(' ', 2);
            var command = parts[0].ToLowerInvariant();

            switch (command)
            {
                case "/quit":
                    logger?.LogEvent("User initiated disconnect");
                    await client.CloseAsync("User quit");
                    goto exitLoop;

                case "/nick":
                    if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
                    {
                        ConsoleHelper.WriteWarning("Usage: /nick <new_nickname>");
                        continue;
                    }
                    var newNick = parts[1].Trim();
                    var oldNick = nickname;
                    await client.ChangeNicknameAsync(oldNick, newNick);
                    nickname = newNick;
                    logger?.LogEvent($"Changed nickname: {oldNick} -> {newNick}");
                    ConsoleHelper.WriteSuccess($"Nickname changed to '{newNick}'");
                    continue;

                case "/help":
                    ConsoleHelper.WriteSystem("Available commands:");
                    ConsoleHelper.WriteSystem("  /nick <name>  - Change your nickname");
                    ConsoleHelper.WriteSystem("  /quit         - Disconnect and exit");
                    ConsoleHelper.WriteSystem("  /help         - Show this help");
                    continue;

                default:
                    ConsoleHelper.WriteWarning($"Unknown command: {command}. Type /help for available commands.");
                    continue;
            }
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

exitLoop:
logger?.Dispose();
client?.Dispose();
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
    Console.WriteLine();
    Console.WriteLine("In-chat commands:");
    Console.WriteLine("  /nick <name>            Change your nickname");
    Console.WriteLine("  /quit                   Disconnect and exit");
    Console.WriteLine("  /help                   Show available commands");
}
