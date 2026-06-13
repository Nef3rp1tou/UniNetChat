using UniNetChat.Protocol;
using UniNetChat.Protocol.Messages;
using UniNetChat.Protocol.Parsing;
using Xunit;

namespace UniNetChat.Protocol.Tests;

public class MessageParserTests
{
    [Fact]
    public void Parse_ValidDiscoverMessage_ReturnsCorrectType()
    {
        var requestId = Guid.NewGuid();
        var raw = $"LNCP/1.0 DISCOVER\r\nRequest-Id: {requestId}\r\nNickname: Alice\r\nFrom: Bob\r\nPort: 5001\r\nDeadline: 2024-01-15T10:30:00Z\r\n\r\n";

        var message = MessageParser.Parse(raw);

        Assert.IsType<DiscoverMessage>(message);
        var discover = (DiscoverMessage)message;
        Assert.Equal("Alice", discover.Nickname);
        Assert.Equal("Bob", discover.FromNickname);
        Assert.Equal(5001, discover.Port);
        Assert.Equal(requestId, discover.RequestId);
    }

    [Fact]
    public void Parse_ValidTextMessage_ExtractsBody()
    {
        var requestId = Guid.NewGuid();
        var raw = $"LNCP/1.0 MSG\r\nRequest-Id: {requestId}\r\nFrom: Alice\r\nTimestamp: 2024-01-15T10:30:00Z\r\nSequence: 5\r\n\r\nHello, World!";

        var message = MessageParser.Parse(raw);

        Assert.IsType<TextMessage>(message);
        var text = (TextMessage)message;
        Assert.Equal("Alice", text.From);
        Assert.Equal(5, text.Sequence);
        Assert.Equal("Hello, World!", text.Text);
    }

    [Fact]
    public void Parse_MultilineBody_PreservesNewlines()
    {
        var requestId = Guid.NewGuid();
        var raw = $"LNCP/1.0 MSG\r\nRequest-Id: {requestId}\r\nFrom: Alice\r\nTimestamp: 2024-01-15T10:30:00Z\r\nSequence: 1\r\n\r\nLine 1\r\nLine 2\r\nLine 3";

        var message = MessageParser.Parse(raw);

        var text = (TextMessage)message;
        Assert.Equal("Line 1\r\nLine 2\r\nLine 3", text.Text);
    }

    [Fact]
    public void Parse_InvalidProtocolVersion_ThrowsFormatException()
    {
        var raw = "HTTP/1.1 DISCOVER\r\nRequest-Id: 123\r\n\r\n";

        Assert.Throws<FormatException>(() => MessageParser.Parse(raw));
    }

    [Fact]
    public void Parse_MissingRequestId_ThrowsFormatException()
    {
        var raw = "LNCP/1.0 DISCOVER\r\nNickname: Alice\r\n\r\n";

        Assert.Throws<FormatException>(() => MessageParser.Parse(raw));
    }

    [Fact]
    public void Parse_UnknownMessageType_ThrowsArgumentException()
    {
        var raw = $"LNCP/1.0 INVALID\r\nRequest-Id: {Guid.NewGuid()}\r\n\r\n";

        Assert.Throws<ArgumentException>(() => MessageParser.Parse(raw));
    }

    [Fact]
    public void Parse_HeartbeatMessage_ReturnsCorrectType()
    {
        var requestId = Guid.NewGuid();
        var raw = $"LNCP/1.0 HEARTBEAT\r\nRequest-Id: {requestId}\r\n\r\n";

        var message = MessageParser.Parse(raw);

        Assert.IsType<HeartbeatMessage>(message);
        Assert.Equal(requestId, message.RequestId);
    }

    [Fact]
    public void Parse_RejectMessage_ParsesReasonCode()
    {
        var requestId = Guid.NewGuid();
        var raw = $"LNCP/1.0 REJECT\r\nRequest-Id: {requestId}\r\nReason: uuid_mismatch:Invalid session\r\n\r\n";

        var message = MessageParser.Parse(raw);

        Assert.IsType<RejectMessage>(message);
        var reject = (RejectMessage)message;
        Assert.Equal(RejectReason.UuidMismatch, reject.ReasonCode);
        Assert.Equal("Invalid session", reject.ReasonText);
    }

    [Fact]
    public void TryParse_InvalidMessage_ReturnsNullWithError()
    {
        var message = MessageParser.TryParse("garbage", out var error);

        Assert.Null(message);
        Assert.NotNull(error);
    }

    [Fact]
    public void RoundTrip_AllMessageTypes_PreservesData()
    {
        var requestId = Guid.NewGuid();
        var messages = new LncpMessage[]
        {
            new DiscoverMessage("Alice", "Bob", 5001, 30) { RequestId = requestId },
            new ConnectMessage(requestId, "Alice"),
            new AcceptMessage(requestId, "Bob"),
            new RejectMessage(requestId, RejectReason.DeadlineExpired),
            new TextMessage(requestId, "Alice", "Test message", 1),
            new AckMessage(requestId, 1),
            new CloseMessage(requestId, "Done"),
            new ClosedMessage(requestId),
            new HeartbeatMessage(requestId),
            new HeartbeatAckMessage(requestId)
        };

        foreach (var original in messages)
        {
            var serialized = MessageSerializer.Serialize(original);
            var parsed = MessageParser.Parse(serialized);

            Assert.Equal(original.Type, parsed.Type);
            Assert.Equal(original.RequestId, parsed.RequestId);
        }
    }
}
