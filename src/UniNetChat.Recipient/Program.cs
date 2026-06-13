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
server.NicknameChanged += (oldNick, newNick) =>
{
    ConsoleHelper.WriteStatus($"Peer changed nickname: {oldNick} -> {newNick}");
    logger?.LogEvent($"Peer renamed: {oldNick} -> {newNick}");
};

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// Start listening in background
var listenTask = Task.Run(() => server.StartListeningAsync(cts.Token));

ConsoleHelper.WriteStatus("Waiting for connections...");
ConsoleHelper.WriteSystem("Commands: /nick <name>, /help, /quit");
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

        // Handle commands
        if (input.StartsWith("/"))
        {
            var parts = input.Split(' ', 2);
            var command = parts[0].ToLowerInvariant();

            switch (command)
            {
                case "/quit":
                    if (server.IsConnected)
                    {
                        logger?.LogEvent("User initiated disconnect");
                        await server.CloseAsync("User quit");
                    }
                    cts.Cancel();
                    goto exitLoop;

                case "/nick":
                    if (!server.IsConnected)
                    {
                        ConsoleHelper.WriteWarning("Not connected. Cannot change nickname.");
                        continue;
                    }
                    if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
                    {
                        ConsoleHelper.WriteWarning("Usage: /nick <new_nickname>");
                        continue;
                    }
                    var newNick = parts[1].Trim();
                    var oldNick = nickname;
                    await server.ChangeNicknameAsync(oldNick, newNick);
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

exitLoop:
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
    Console.WriteLine();
    Console.WriteLine("In-chat commands:");
    Console.WriteLine("  /nick <name>            Change your nickname");
    Console.WriteLine("  /quit                   Disconnect and exit");
    Console.WriteLine("  /help                   Show available commands");
}
