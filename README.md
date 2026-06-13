# Uni-Net Chat

A local network chat application using a custom application-layer protocol called **LNCP (Local Network Chat Protocol)**.

## Features

| Feature | Description |
|---------|-------------|
| **Zero Configuration** | UDP broadcast discovery - no IP addresses needed |
| **Reliable Messaging** | TCP with acknowledgments for every message |
| **Heartbeat Detection** | Detects dead connections automatically |
| **Nickname Change** | Change your nickname mid-chat with `/nick` |
| **Chat Logging** | All sessions saved to local log files |
| **Colored Console** | Visual distinction for status, errors, incoming/outgoing |
| **Unit Tested** | 36 tests covering parser, serializer, state machine |
| **Retry on Failure** | Re-enter target nickname if discovery fails |

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         UniNetChat                              │
├──────────────────────────┬──────────────────────────────────────┤
│   UniNetChat.Protocol    │   Shared library                     │
│   ├── Messages/          │   12 message types                   │
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
- Use commands below

## In-Chat Commands

| Command | Description |
|---------|-------------|
| `/nick <name>` | Change your nickname |
| `/help` | Show available commands |
| `/quit` | Disconnect and exit |

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

### Message Types (12)

| Type | Purpose |
|------|---------|
| DISCOVER | UDP broadcast to find recipient |
| CONNECT | Recipient connects to initiator |
| ACCEPT / REJECT | Connection response |
| MSG / ACK | Chat messages with acknowledgment |
| CLOSE / CLOSED | Graceful termination |
| HEARTBEAT / HEARTBEAT_ACK | Connection health |
| NICK_CHANGE / NICK_ACK | Nickname updates |

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

- [Protocol Specification](docs/PROTOCOL_SPECIFICATION.md) - Full protocol details
- [Diagrams](docs/diagrams/) - Sequence, state machine, architecture diagrams

## Design Rationale

| Choice | Why |
|--------|-----|
| HTTP-style headers | Human-readable, debuggable, no JSON parser needed |
| Length-prefix framing | Supports multi-line messages and binary content |
| Separate library | Reusable, testable, maintainable |
| Explicit error codes | Machine-readable for automation |
| Heartbeat messages | TCP keepalive default is 2 hours - too slow |
