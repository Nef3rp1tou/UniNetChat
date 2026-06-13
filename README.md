# Uni-Net Chat

A local network chat application using a custom application-layer protocol called **LNCP (Local Network Chat Protocol)**.

## Features

| Feature | Description |
|---------|-------------|
| **Zero Configuration** | UDP broadcast discovery - no IP addresses needed |
| **Reliable Messaging** | TCP with acknowledgments for every message |
| **Heartbeat Detection** | Detects dead connections within 30 seconds |
| **Chat Logging** | All sessions saved to local log files |
| **Colored Console** | Visual distinction for status, errors, incoming/outgoing |
| **Unit Tested** | 36 tests covering parser, serializer, state machine |
| **Explicit Error Codes** | Machine-readable reject reasons for automation |
| **Multi-line Messages** | Length-prefix framing supports any content |

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         UniNetChat                              │
├──────────────────────────┬──────────────────────────────────────┤
│   UniNetChat.Protocol    │   Shared library                     │
│   ├── Messages/          │   10 message types                   │
│   ├── Parsing/           │   Parser + Serializer                │
│   └── States/            │   State machine                      │
├──────────────────────────┼──────────────────────────────────────┤
│   UniNetChat.Initiator   │   Starts chat sessions               │
├──────────────────────────┼──────────────────────────────────────┤
│   UniNetChat.Recipient   │   Receives chat requests             │
├──────────────────────────┼──────────────────────────────────────┤
│   UniNetChat.Protocol.   │   36 unit tests                      │
│   Tests                  │   xUnit framework                    │
└──────────────────────────┴──────────────────────────────────────┘
```

## Requirements

- .NET 8.0 SDK or later
- Two machines on the same local network (or two terminals for testing)
- UDP port 5000, TCP port 5001 open

## Quick Start

### Build

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Terminal 1 - Start Recipient

```bash
dotnet run --project src/UniNetChat.Recipient -- -n Alice
```

### Terminal 2 - Start Initiator

```bash
dotnet run --project src/UniNetChat.Initiator -- -n Bob -t Alice
```

### Chat!

- Type messages and press Enter
- Type `/quit` to disconnect

## Command Line Options

### Initiator

```
-n, --nickname <name>   Your nickname (default: Initiator)
-t, --target <name>     Target recipient's nickname
-p, --port <port>       TCP port to listen on (default: 5001)
--no-log                Disable chat logging
-h, --help              Show help message
```

### Recipient

```
-n, --nickname <name>   Your nickname (default: Recipient)
--no-log                Disable chat logging
-h, --help              Show help message
```

## Protocol Highlights

### Message Format (HTTP-style)

```
LNCP/1.0 MSG
Request-Id: 550e8400-e29b-41d4-a716-446655440000
From: Alice
Timestamp: 2024-01-15T10:31:15Z
Sequence: 1

Hello, this message can
span multiple lines!
```

### Message Types

| Type | Purpose |
|------|---------|
| DISCOVER | UDP broadcast to find recipient |
| CONNECT | Recipient connects to initiator |
| ACCEPT | Connection accepted |
| REJECT | Connection rejected with reason code |
| MSG | Chat text message |
| ACK | Message acknowledgment |
| CLOSE | Graceful termination request |
| CLOSED | Termination acknowledged |
| HEARTBEAT | Connection liveness check |
| HEARTBEAT_ACK | Heartbeat response |

### Error Codes

| Code | Description |
|------|-------------|
| `uuid_mismatch` | Wrong session ID |
| `deadline_expired` | Too late to connect |
| `malformed_message` | Parse error |
| `user_declined` | Manual rejection |
| `already_connected` | Busy in another session |

## Chat Logs

Sessions are logged to:
```
%LOCALAPPDATA%\UniNetChat\logs\chat_<local>_<remote>_<timestamp>.log
```

## Documentation

See [docs/PROTOCOL_SPECIFICATION.md](docs/PROTOCOL_SPECIFICATION.md) for the complete protocol specification including:

- Design rationale and comparisons
- Message format and examples
- State machine diagrams
- Error handling matrix
- Security considerations

## Why This Design?

| Choice | Rationale |
|--------|-----------|
| HTTP-style headers | Human-readable, no JSON parser needed |
| Length-prefix framing | Supports multi-line messages and binary |
| Separate library | Reusable, testable, maintainable |
| Explicit error codes | Machine-readable for automation |
| Heartbeat messages | Detects dead connections (TCP keepalive is 2 hours!) |

## License

MIT License
