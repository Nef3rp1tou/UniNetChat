# LNCP (Local Network Chat Protocol) Specification v1.0

## 1. Overview

LNCP is a custom application-layer protocol designed for peer-to-peer text chat over local networks. It combines UDP broadcast for zero-configuration discovery with TCP for reliable message delivery.

### Design Goals

| Goal | How Achieved |
|------|--------------|
| Zero configuration | UDP broadcast discovery - no IP addresses needed |
| Reliability | TCP for all chat messages with acknowledgments |
| Human readability | HTTP-style headers for easy debugging |
| Binary safety | Length-prefix framing allows any payload content |
| Extensibility | Custom headers and protocol versioning |
| Robustness | Heartbeat detection, explicit error codes, state machine |

---

## 2. Design Rationale

### Why HTTP-Style Headers Instead of JSON?

| Aspect | HTTP-Style Headers | JSON (like competitor) |
|--------|-------------------|------------------------|
| **Parsing** | Line-by-line, no external library | Requires JSON parser |
| **Debugging** | Readable in Wireshark/tcpdump | Readable but dense |
| **Body handling** | Natural separation of metadata/content | Body mixed in structure |
| **Binary payloads** | Trivial - body after headers | Must Base64 encode |
| **Streaming** | Can process headers before body arrives | Must parse entire message |

HTTP/1.1, SMTP, SIP, and RTSP all use this format because it balances human readability with parsing efficiency. JSON is popular for APIs but adds parsing overhead and conflates metadata with payload.

### Why Length-Prefix Framing Instead of Newline Delimiters?

| Aspect | Length-Prefix (4 bytes + data) | Newline Delimiter |
|--------|-------------------------------|-------------------|
| **Multi-line messages** | Fully supported | Cannot contain newlines |
| **Binary data** | Fully supported | Breaks on 0x0A bytes |
| **Parse efficiency** | Know exact bytes to read | Must scan for delimiter |
| **Buffer management** | Pre-allocate exact size | Grow buffer dynamically |

Newline framing (NDJSON) is simpler but fundamentally limited. A chat message like "Line 1\nLine 2" would be split into two messages. Length-prefix is used by PostgreSQL, MySQL, and most binary protocols.

### Why Explicit Error Codes Instead of Free-Text Reasons?

```
Our approach:     Reason: uuid_mismatch:Additional details here
Competitor:       reason: "Invalid session ID"
```

Machine-readable codes (`uuid_mismatch`, `deadline_expired`, `malformed_message`) enable:
- Automated error handling and retry logic
- Internationalization (display localized messages)
- Metrics and monitoring aggregation
- Programmatic decision making

Free-text reasons are human-friendly but require string matching for automation.

### Why Heartbeat Messages?

TCP's built-in keepalive has serious limitations:
- Default timeout is **2 hours** (7200 seconds)
- Detected by OS, not application
- No application-level health check

LNCP heartbeats provide:
- Configurable interval (default 30 seconds)
- Application-level liveness detection
- Round-trip latency measurement
- NAT traversal keepalive

### Why Separate Library + Applications Architecture?

```
UniNetChat/
├── src/
│   ├── UniNetChat.Protocol/     ← Reusable library
│   ├── UniNetChat.Initiator/    ← Console app
│   └── UniNetChat.Recipient/    ← Console app
└── tests/
    └── UniNetChat.Protocol.Tests/  ← Unit tests
```

| Benefit | Description |
|---------|-------------|
| **Reusability** | Protocol can be embedded in GUI apps, bots, etc. |
| **Testability** | Parser/serializer tested independently of I/O |
| **Separation of Concerns** | Network code separate from UI code |
| **Maintainability** | Change protocol without touching apps |

Single-file scripts are faster to write but don't scale.

---

## 3. Message Format

All LNCP messages follow HTTP-style structure:

```
LNCP/1.0 <MESSAGE_TYPE>\r\n
<Header-Name>: <Header-Value>\r\n
...
\r\n
<Body>
```

### TCP Framing

TCP messages use length-prefix framing:

```
┌──────────────────┬─────────────────────────────┐
│ Length (4 bytes) │ Message (N bytes)           │
│ Little-endian    │ UTF-8 encoded               │
└──────────────────┴─────────────────────────────┘
```

### UDP Messages

UDP datagrams are self-delimiting (no length prefix needed).

---

## 4. Message Types

| Type | Transport | Direction | Purpose |
|------|-----------|-----------|---------|
| `DISCOVER` | UDP | Broadcast | Find recipient on network |
| `CONNECT` | TCP | R → I | Recipient initiates handshake |
| `ACCEPT` | TCP | I → R | Initiator accepts connection |
| `REJECT` | TCP | I → R | Initiator rejects with reason code |
| `MSG` | TCP | Bidirectional | Chat text message |
| `ACK` | TCP | Bidirectional | Message acknowledgment |
| `CLOSE` | TCP | Bidirectional | Graceful termination request |
| `CLOSED` | TCP | Bidirectional | Termination acknowledgment |
| `HEARTBEAT` | TCP | Bidirectional | Connection liveness check |
| `HEARTBEAT_ACK` | TCP | Bidirectional | Heartbeat response |
| `NICK_CHANGE` | TCP | Bidirectional | Notify peer of nickname change |
| `NICK_ACK` | TCP | Bidirectional | Acknowledge nickname change |

**Legend**: I = Initiator, R = Recipient

---

## 5. Header Fields

| Header | Used In | Description |
|--------|---------|-------------|
| `Request-Id` | All | UUID v4 identifying the session |
| `Nickname` | DISCOVER, CONNECT, ACCEPT | User's display name |
| `From` | DISCOVER, MSG | Sender's nickname |
| `Deadline` | DISCOVER | ISO 8601 timestamp for response deadline |
| `Port` | DISCOVER | TCP port initiator is listening on |
| `Timestamp` | MSG, ACK | ISO 8601 message timestamp |
| `Sequence` | MSG, ACK | Message sequence number (1-based) |
| `Old-Nickname` | NICK_CHANGE | Previous nickname |
| `New-Nickname` | NICK_CHANGE | New nickname |
| `Reason` | REJECT, CLOSE | Error code with optional details |

---

## 6. Reject Reason Codes

| Code | Description |
|------|-------------|
| `uuid_mismatch` | Request-Id doesn't match discovery UUID |
| `deadline_expired` | Connection attempt after deadline |
| `malformed_message` | Unparseable message received |
| `user_declined` | User manually rejected connection |
| `already_connected` | Recipient is in another session |
| `unknown` | Unspecified error |

Format: `<code>` or `<code>:<human-readable details>`

Example: `deadline_expired:Request was 45 seconds late`

---

## 7. Message Examples

### DISCOVER

```
LNCP/1.0 DISCOVER
Request-Id: 550e8400-e29b-41d4-a716-446655440000
Nickname: Alice
From: Bob
Port: 5001
Deadline: 2024-01-15T10:30:00Z

```

### MSG

```
LNCP/1.0 MSG
Request-Id: 550e8400-e29b-41d4-a716-446655440000
From: Alice
Timestamp: 2024-01-15T10:31:15Z
Sequence: 1

Hello, how are you?
This message can span
multiple lines!
```

### REJECT

```
LNCP/1.0 REJECT
Request-Id: 550e8400-e29b-41d4-a716-446655440000
Reason: deadline_expired:Connection attempt was 30 seconds late

```

### HEARTBEAT

```
LNCP/1.0 HEARTBEAT
Request-Id: 550e8400-e29b-41d4-a716-446655440000

```

### NICK_CHANGE

```
LNCP/1.0 NICK_CHANGE
Request-Id: 550e8400-e29b-41d4-a716-446655440000
Old-Nickname: Alice
New-Nickname: Alice_AFK

```

### NICK_ACK

```
LNCP/1.0 NICK_ACK
Request-Id: 550e8400-e29b-41d4-a716-446655440000
Nickname: Alice_AFK

```

---

## 8. State Machine

### Initiator States

```
IDLE ──[Start]──→ DISCOVERING ──[Broadcast sent]──→ WAITING_CONNECTION
                       │                                    │
                       ↓                                    ↓
                    (timeout)                         HANDSHAKING
                       │                                    │
                       ↓                         ┌──────────┴──────────┐
                     IDLE                        ↓                    ↓
                                            CONNECTED              CLOSED
                                                 │                    ↑
                                                 ↓                    │
                                             CLOSING ─────────────────┘
```

### Recipient States

```
LISTENING ──[DISCOVER received]──→ CONNECTING ──[TCP connected]──→ HANDSHAKING
     ↑                                  │                              │
     │                              (failed)                   ┌───────┴───────┐
     │                                  │                      ↓               ↓
     │                                  ↓                 CONNECTED         CLOSED
     │                             LISTENING                   │               │
     │                                                         ↓               │
     └─────────────────────────────────────────────────────CLOSING────────────┘
```

---

## 9. Communication Flow

```
Initiator                                                    Recipient
    │                                                            │
    │──────────────── DISCOVER (UDP broadcast) ─────────────────→│
    │                                                            │
    │←═══════════════════ TCP Connection ════════════════════════│
    │                                                            │
    │←────────────────────── CONNECT ────────────────────────────│
    │                                                            │
    │─────────────────────── ACCEPT ────────────────────────────→│
    │                                                            │
    │←──────────────────────── MSG ──────────────────────────────│
    │───────────────────────── ACK ─────────────────────────────→│
    │                                                            │
    │───────────────────────── MSG ─────────────────────────────→│
    │←──────────────────────── ACK ──────────────────────────────│
    │                                                            │
    │←─────────────────────HEARTBEAT ────────────────────────────│
    │──────────────────── HEARTBEAT_ACK ────────────────────────→│
    │                                                            │
    │────────────────────── CLOSE ──────────────────────────────→│
    │←───────────────────── CLOSED ──────────────────────────────│
    │                                                            │
```

---

## 10. Error Handling

| Scenario | Detected By | Response | Recovery |
|----------|-------------|----------|----------|
| UUID mismatch | Initiator | `REJECT uuid_mismatch` | Close TCP |
| Deadline expired | Initiator | `REJECT deadline_expired` | Close TCP |
| Malformed message | Either | `CLOSE malformed_message` | Close TCP |
| Unknown message type | Either | `CLOSE` | Close TCP |
| Wrong protocol version | Either | Discard (UDP) / Close (TCP) | - |
| Heartbeat timeout | Either | `CLOSE` | Reconnect |
| TCP connection reset | Either | - | Return to IDLE/LISTENING |
| Message too large | Receiver | `CLOSE` | Truncate or reject |

---

## 11. Ports

| Port | Protocol | Purpose |
|------|----------|---------|
| 5000 | UDP | Discovery broadcasts |
| 5001 | TCP | Default chat port (configurable) |

---

## 12. Encoding Rules

1. All text is UTF-8 encoded
2. Line endings are CRLF (`\r\n`)
3. Headers are case-insensitive for names
4. Timestamps use ISO 8601 format with UTC (`2024-01-15T10:30:00Z`)
5. UUIDs use lowercase with hyphens (`550e8400-e29b-41d4-a716-446655440000`)
6. Maximum message size: 65,536 bytes
7. Maximum text message body: 32,000 characters
8. Sequence numbers start at 1, increment per message

---

## 13. Security Considerations

LNCP v1.0 is designed for **trusted local networks** and does not include:
- Authentication (any client can claim any nickname)
- Encryption (messages are plaintext)
- Integrity verification (no signatures or checksums)

For production use, consider:
- Running over a VPN
- Adding TLS encryption (LNCP over TLS)
- Implementing challenge-response authentication

---

## 14. Comparison with Alternative Approaches

| Feature | LNCP (This Protocol) | JSON-over-Newline | Raw TCP |
|---------|---------------------|-------------------|---------|
| Multi-line messages | ✓ Length-prefix | ✗ Breaks | ✓ |
| Binary payloads | ✓ | ✗ Must Base64 | ✓ |
| Human debugging | ✓ HTTP-style | ✓ JSON | ✗ |
| No external deps | ✓ | Needs JSON parser | ✓ |
| Extensible headers | ✓ | ✓ | ✗ |
| Connection health | ✓ Heartbeat | ✗ | ✗ |
| Error codes | ✓ Machine-readable | Free text only | ✗ |
| Unit testable | ✓ Separate library | ✗ Script-based | ✗ |

---

## 15. Implementation Features

### Chat Logging

All sessions are logged to:
```
%LOCALAPPDATA%\UniNetChat\logs\chat_<local>_<remote>_<timestamp>.log
```

### Colored Console Output

- **Cyan**: Status messages
- **Green**: Success messages
- **Yellow**: Warnings and prompts
- **Red**: Error messages
- **Magenta**: Incoming messages
- **White**: Outgoing messages

### Unit Test Coverage

The protocol library includes comprehensive unit tests:
- Message serialization round-trips
- Parser error handling
- State transition validation
- Reject reason parsing

---

## 16. References

This protocol design was informed by:

- **RFC 2616** - HTTP/1.1 (header format)
- **RFC 5321** - SMTP (text protocol design)
- **RFC 4122** - UUID format
- **RFC 3261** - SIP (UDP discovery + TCP session)
- **RFC 8259** - JSON (for comparison)
