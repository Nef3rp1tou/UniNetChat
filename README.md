# Uni-Net Chat

A local network chat application using a custom application-layer protocol called **LNCP (Local Network Chat Protocol)**.

## Overview

Uni-Net Chat enables peer-to-peer messaging between two computers on the same local network. It uses UDP broadcast for discovery and TCP for reliable message exchange.

## Projects

- **UniNetChat.Protocol** - Shared library containing the LNCP protocol implementation
- **UniNetChat.Initiator** - Console application that initiates chat sessions
- **UniNetChat.Recipient** - Console application that receives chat requests

## Requirements

- .NET 8.0 SDK or later
- Two machines on the same local network (or two terminals on the same machine for testing)

## Building

```bash
dotnet build
```

## Usage

### Starting the Recipient

On the machine that will receive chat requests:

```bash
cd src/UniNetChat.Recipient
dotnet run -- --nickname "Alice"
```

### Starting the Initiator

On the machine that will initiate the chat:

```bash
cd src/UniNetChat.Initiator
dotnet run -- --nickname "Bob" --target "Alice"
```

### Chatting

Once connected, type messages and press Enter to send. Type `/quit` to close the connection.

## Protocol

See [docs/PROTOCOL_SPECIFICATION.md](docs/PROTOCOL_SPECIFICATION.md) for the full LNCP protocol specification.

## License

MIT License
