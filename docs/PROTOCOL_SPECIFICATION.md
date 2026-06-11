# LNCP (Local Network Chat Protocol) Specification v1.0

## Overview

LNCP is a simple, text-based application-layer protocol designed for peer-to-peer chat communication over local networks. It combines UDP for discovery and TCP for reliable message exchange.

## Design Goals

- Simple, human-readable format for easy debugging
- Clear message boundaries using length-prefixed framing
- Stateful connections with defined state transitions
- Binary-safe payload support with text-based headers

## Message Format

All LNCP messages follow this structure:

```
LNCP/1.0 <MESSAGE_TYPE>\r\n
<Header-Name>: <Header-Value>\r\n
...
\r\n
<Body>
```

### Components

- **Protocol Version**: Always `LNCP/1.0`
- **Message Type**: One of the defined message types (see below)
- **Headers**: Key-value pairs separated by `: ` (colon and space)
- **Separator**: Empty line (`\r\n`) between headers and body
- **Body**: Optional message content (used for text messages)

### Framing

TCP messages use length-prefix framing:
- 4 bytes: Message length (little-endian int32)
- N bytes: Message content

UDP messages do not use length prefixes (datagram boundaries are preserved).

## Message Types

| Type | Transport | Direction | Purpose |
|------|-----------|-----------|---------|
| `DISCOVER` | UDP | Broadcast | Find recipient on network |
| `CONNECT` | TCP | R → I | Recipient initiates TCP handshake |
| `ACCEPT` | TCP | I → R | Initiator accepts connection |
| `REJECT` | TCP | I → R | Initiator rejects connection |
| `MSG` | TCP | Bidirectional | Chat text message |
| `ACK` | TCP | Bidirectional | Message acknowledgment |
| `CLOSE` | TCP | Bidirectional | Connection termination request |
| `CLOSED` | TCP | Bidirectional | Connection termination acknowledgment |

**Legend**: I = Initiator, R = Recipient

## Header Fields

| Header | Required In | Description |
|--------|-------------|-------------|
| `Request-Id` | All | UUID identifying the session |
| `Nickname` | DISCOVER, CONNECT, ACCEPT | User's display name |
| `From` | DISCOVER, MSG | Sender's nickname |
| `Deadline` | DISCOVER | ISO 8601 timestamp for response deadline |
| `Port` | DISCOVER | TCP port initiator is listening on |
| `Timestamp` | MSG, ACK | ISO 8601 message timestamp |
| `Sequence` | MSG, ACK | Message sequence number |
| `Reason` | REJECT, CLOSE | Human-readable reason |

## Message Definitions

### DISCOVER

Broadcast to find a recipient on the network.

```
LNCP/1.0 DISCOVER\r\n
Request-Id: <uuid>\r\n
Nickname: <target-nickname>\r\n
From: <sender-nickname>\r\n
Port: <tcp-port>\r\n
Deadline: <iso8601-timestamp>\r\n
\r\n
```

### CONNECT

Recipient connects to initiator's TCP port.

```
LNCP/1.0 CONNECT\r\n
Request-Id: <uuid>\r\n
Nickname: <recipient-nickname>\r\n
\r\n
```

### ACCEPT

Initiator accepts the connection.

```
LNCP/1.0 ACCEPT\r\n
Request-Id: <uuid>\r\n
Nickname: <initiator-nickname>\r\n
\r\n
```

### REJECT

Initiator rejects the connection.

```
LNCP/1.0 REJECT\r\n
Request-Id: <uuid>\r\n
Reason: <human-readable-reason>\r\n
\r\n
```

### MSG

Chat text message.

```
LNCP/1.0 MSG\r\n
Request-Id: <uuid>\r\n
From: <sender-nickname>\r\n
Timestamp: <iso8601-timestamp>\r\n
Sequence: <sequence-number>\r\n
\r\n
<message-text>
```

### ACK

Acknowledgment of received message.

```
LNCP/1.0 ACK\r\n
Request-Id: <uuid>\r\n
Timestamp: <iso8601-timestamp>\r\n
Sequence: <sequence-number>\r\n
\r\n
```

### CLOSE

Request to close the connection.

```
LNCP/1.0 CLOSE\r\n
Request-Id: <uuid>\r\n
Reason: <optional-reason>\r\n
\r\n
```

### CLOSED

Acknowledgment of connection closure.

```
LNCP/1.0 CLOSED\r\n
Request-Id: <uuid>\r\n
\r\n
```

## State Machine

### Initiator States

```
IDLE
  │
  ├──[Start discovery]──→ DISCOVERING
  │                           │
  │                           ├──[Timeout]──→ IDLE
  │                           │
  │                           └──[Broadcast sent]──→ WAITING_CONNECTION
  │                                                       │
  │                                                       ├──[Timeout]──→ IDLE
  │                                                       │
  │                                                       └──[TCP connected]──→ HANDSHAKING
  │                                                                                  │
  │                                                                                  ├──[Invalid UUID]──→ CLOSED
  │                                                                                  │
  │                                                                                  └──[Valid CONNECT]──→ CONNECTED
  │                                                                                                           │
  │                                                                                                           ├──[CLOSE sent]──→ CLOSING
  │                                                                                                           │                     │
  │                                                                                                           │                     └──[CLOSED received]──→ CLOSED
  │                                                                                                           │
  │                                                                                                           └──[CLOSE received]──→ CLOSED
  │
  └──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────→ CLOSED
```

### Recipient States

```
LISTENING
  │
  ├──[DISCOVER received]──→ CONNECTING
  │                             │
  │                             ├──[Connection failed]──→ LISTENING
  │                             │
  │                             └──[TCP connected]──→ HANDSHAKING
  │                                                       │
  │                                                       ├──[REJECT received]──→ CLOSED ──→ LISTENING
  │                                                       │
  │                                                       └──[ACCEPT received]──→ CONNECTED
  │                                                                                   │
  │                                                                                   ├──[CLOSE sent]──→ CLOSING
  │                                                                                   │                     │
  │                                                                                   │                     └──[CLOSED received]──→ CLOSED ──→ LISTENING
  │                                                                                   │
  │                                                                                   └──[CLOSE received]──→ CLOSED ──→ LISTENING
```

## Communication Flow

```
Initiator                          Network                         Recipient
    │                                 │                                 │
    │───────── DISCOVER (UDP) ───────→│───────── DISCOVER (UDP) ───────→│
    │         (broadcast)             │                                 │
    │                                 │                                 │
    │←════════════════════════════════│════ TCP Connection ════════════│
    │                                 │                                 │
    │←─────────────────────── CONNECT ──────────────────────────────────│
    │                                 │                                 │
    │─────────────────────── ACCEPT ───────────────────────────────────→│
    │                                 │                                 │
    │←─────────────────────── MSG ──────────────────────────────────────│
    │─────────────────────── ACK ──────────────────────────────────────→│
    │                                 │                                 │
    │─────────────────────── MSG ──────────────────────────────────────→│
    │←─────────────────────── ACK ──────────────────────────────────────│
    │                                 │                                 │
    │─────────────────────── CLOSE ────────────────────────────────────→│
    │←─────────────────────── CLOSED ───────────────────────────────────│
    │                                 │                                 │
```

## Ports

| Port | Protocol | Purpose |
|------|----------|---------|
| 5000 | UDP | Discovery broadcasts |
| 5001 | TCP | Default connection port (configurable) |

## Error Handling

| Scenario | Response | Action |
|----------|----------|--------|
| Invalid Request-Id | REJECT | Close TCP connection |
| Deadline expired | REJECT | Close TCP connection |
| Malformed message | CLOSE | Terminate session |
| Network timeout | Auto-close | Clean up resources |
| Unexpected message | CLOSE | Terminate session |

## Security Considerations

This protocol is designed for trusted local networks and does not include:
- Authentication
- Encryption
- Message integrity verification

For production use, consider:
- Running over a VPN
- Adding TLS encryption
- Implementing authentication

## Encoding

- All text is UTF-8 encoded
- Line endings are CRLF (`\r\n`)
- Timestamps use ISO 8601 format (e.g., `2024-01-15T10:30:00Z`)
- UUIDs use standard format (e.g., `550e8400-e29b-41d4-a716-446655440000`)
