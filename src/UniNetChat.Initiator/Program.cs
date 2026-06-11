using UniNetChat.Initiator;
using UniNetChat.Protocol;

// Parse command line arguments
string nickname = "Initiator";
string? targetNickname = null;
int port = LncpConstants.DefaultTcpPort;

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
        case "--help" or "-h":
            PrintHelp();
            return;
    }
}

Console.WriteLine("=== Uni-Net Chat (Initiator) ===");
Console.WriteLine($"Your nickname: {nickname}");
Console.WriteLine();

if (string.IsNullOrEmpty(targetNickname))
{
    Console.Write("Enter target nickname to find: ");
    targetNickname = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(targetNickname))
    {
        Console.WriteLine("Error: Target nickname is required.");
        return;
    }
}

using var client = new InitiatorClient(nickname, port);

// Set up event handlers
client.StatusChanged += status => Console.WriteLine($"[Status] {status}");
client.MessageReceived += (from, text) => Console.WriteLine($"[{from}] {text}");
client.Disconnected += () => Console.WriteLine("[Disconnected]");

// Discover and connect
Console.WriteLine($"Searching for '{targetNickname}'...");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var connected = await client.DiscoverAndConnectAsync(targetNickname, cancellationToken: cts.Token);

if (!connected)
{
    Console.WriteLine("Failed to connect. Make sure the recipient is running.");
    return;
}

Console.WriteLine();
Console.WriteLine("Connected! Type messages and press Enter to send.");
Console.WriteLine("Type /quit to disconnect.");
Console.WriteLine();

// Main chat loop
while (client.IsConnected && !cts.Token.IsCancellationRequested)
{
    try
    {
        Console.Write($"[{nickname}] ");
        var input = Console.ReadLine();

        if (string.IsNullOrEmpty(input))
        {
            continue;
        }

        if (input.Equals("/quit", StringComparison.OrdinalIgnoreCase))
        {
            await client.CloseAsync("User quit");
            break;
        }

        await client.SendTextAsync(input);
    }
    catch (InvalidOperationException)
    {
        // Disconnected
        break;
    }
    catch (OperationCanceledException)
    {
        break;
    }
}

Console.WriteLine("Goodbye!");

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
    Console.WriteLine("  -h, --help              Show this help message");
}
