using UniNetChat.Recipient;

// Parse command line arguments
string nickname = "Recipient";

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--nickname" or "-n":
            if (i + 1 < args.Length) nickname = args[++i];
            break;
        case "--help" or "-h":
            PrintHelp();
            return;
    }
}

Console.WriteLine("=== Uni-Net Chat (Recipient) ===");
Console.WriteLine($"Your nickname: {nickname}");
Console.WriteLine();

using var server = new RecipientServer(nickname);
using var cts = new CancellationTokenSource();

// Set up event handlers
server.StatusChanged += status => Console.WriteLine($"[Status] {status}");
server.MessageReceived += (from, text) => Console.WriteLine($"[{from}] {text}");
server.ConnectionRequest += (from, endpoint) => Console.WriteLine($"[Request] Connection from '{from}' at {endpoint}");
server.Disconnected += () =>
{
    Console.WriteLine("[Disconnected]");
    Console.WriteLine("Waiting for new connections...");
};

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// Start listening in background
var listenTask = Task.Run(() => server.StartListeningAsync(cts.Token));

Console.WriteLine("Waiting for connections...");
Console.WriteLine("Press Ctrl+C to exit.");
Console.WriteLine();

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
                await server.CloseAsync("User quit");
            }
            cts.Cancel();
            break;
        }

        if (!server.IsConnected)
        {
            Console.WriteLine("Not connected. Waiting for incoming connection...");
            continue;
        }

        Console.Write($"\r[{nickname}] {input}\n");
        await server.SendTextAsync(input);
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

Console.WriteLine("Goodbye!");

static void PrintHelp()
{
    Console.WriteLine("Uni-Net Chat Recipient");
    Console.WriteLine();
    Console.WriteLine("Usage: UniNetChat.Recipient [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -n, --nickname <name>   Your nickname (default: Recipient)");
    Console.WriteLine("  -h, --help              Show this help message");
}
