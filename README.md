# Uni-Net Chat

A local network chat application using a custom application-layer protocol called **LNCP (Local Network Chat Protocol)**.

## Overview

Uni-Net Chat enables peer-to-peer messaging between two computers on the same local network. It uses UDP broadcast for discovery and TCP for reliable message exchange.

### How It Works

1. **Discovery Phase**: The initiator broadcasts a UDP message to find a recipient by nickname
2. **Connection Phase**: The recipient connects to the initiator via TCP
3. **Handshake Phase**: Both parties exchange identity information
4. **Chat Phase**: Messages are exchanged with acknowledgments
5. **Closure Phase**: Either party can gracefully close the connection

## Projects

| Project | Description |
|---------|-------------|
| **UniNetChat.Protocol** | Shared library containing LNCP message types, parser, serializer, and state machine |
| **UniNetChat.Initiator** | Console application that initiates chat sessions |
| **UniNetChat.Recipient** | Console application that receives chat requests |

## Requirements

- .NET 8.0 SDK or later
- Two machines on the same local network (or two terminals on the same machine for testing)
- UDP port 5000 open for discovery
- TCP port 5001 (or custom) open for connections

## Building

```bash
dotnet build
```

## Usage

### Starting the Recipient

On the machine that will receive chat requests, start the recipient first:

```bash
cd src/UniNetChat.Recipient
dotnet run -- --nickname "Alice"
```

The recipient will listen for incoming discovery broadcasts on UDP port 5000.

### Starting the Initiator

On the machine that will initiate the chat:

```bash
cd src/UniNetChat.Initiator
dotnet run -- --nickname "Bob" --target "Alice"
```

The initiator will:
1. Start a TCP listener on port 5001
2. Broadcast a discovery message looking for "Alice"
3. Wait for Alice to connect

### Command Line Options

**Initiator:**
```
  -n, --nickname <name>   Your nickname (default: Initiator)
  -t, --target <name>     Target recipient's nickname
  -p, --port <port>       TCP port to listen on (default: 5001)
  -h, --help              Show help message
```

**Recipient:**
```
  -n, --nickname <name>   Your nickname (default: Recipient)
  -h, --help              Show help message
```

### Chatting

Once connected:
- Type messages and press Enter to send
- Type `/quit` to close the connection gracefully
- Press Ctrl+C to exit

### Testing on a Single Machine

You can test on a single machine by opening two terminal windows:

**Terminal 1 (Recipient):**
```bash
dotnet run --project src/UniNetChat.Recipient -- -n Alice
```

**Terminal 2 (Initiator):**
```bash
dotnet run --project src/UniNetChat.Initiator -- -n Bob -t Alice
```

## Protocol

See [docs/PROTOCOL_SPECIFICATION.md](docs/PROTOCOL_SPECIFICATION.md) for the full LNCP protocol specification.

### Message Types

| Type | Description |
|------|-------------|
| DISCOVER | UDP broadcast to find recipient |
| CONNECT | Recipient connects to initiator |
| ACCEPT | Initiator accepts connection |
| REJECT | Initiator rejects connection |
| MSG | Chat text message |
| ACK | Message acknowledgment |
| CLOSE | Request to close connection |
| CLOSED | Acknowledgment of closure |

## Troubleshooting

### Discovery Not Working

- Ensure both machines are on the same network subnet
- Check firewall settings for UDP port 5000
- Verify the recipient is running before starting the initiator

### Connection Failed

- Ensure TCP port 5001 is not blocked by firewall
- Check that the nickname matches exactly (case-insensitive)
- Verify the discovery deadline hasn't expired

### Messages Not Sending

- Ensure the connection is established (look for "Connected" status)
- Check for network connectivity issues

## License

MIT License
