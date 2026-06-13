using UniNetChat.Protocol;
using UniNetChat.Protocol.Messages;
using UniNetChat.Protocol.Parsing;
using Xunit;

namespace UniNetChat.Protocol.Tests;

public class MessageSerializerTests
{
    [Fact]
    public void Serialize_DiscoverMessage_ContainsAllFields()
    {
        var message = new DiscoverMessage("Alice", "Bob", 5001, 30);
        var serialized = MessageSerializer.Serialize(message);

        Assert.Contains("LNCP/1.0 DISCOVER", serialized);
        Assert.Contains("Nickname: Alice", serialized);
        Assert.Contains("From: Bob", serialized);
        Assert.Contains("Port: 5001", serialized);
        Assert.Contains("Request-Id:", serialized);
        Assert.Contains("Deadline:", serialized);
    }

    [Fact]
    public void Serialize_TextMessage_IncludesBody()
    {
        var requestId = Guid.NewGuid();
        var message = new TextMessage(requestId, "Bob", "Hello, World!", 1);
        var serialized = MessageSerializer.Serialize(message);

        Assert.Contains("LNCP/1.0 MSG", serialized);
        Assert.Contains("From: Bob", serialized);
        Assert.Contains("Sequence: 1", serialized);
        Assert.EndsWith("Hello, World!", serialized);
    }

    [Fact]
    public void Serialize_RejectMessage_ContainsReasonCode()
    {
        var requestId = Guid.NewGuid();
        var message = new RejectMessage(requestId, RejectReason.DeadlineExpired, "Too late");
        var serialized = MessageSerializer.Serialize(message);

        Assert.Contains("LNCP/1.0 REJECT", serialized);
        Assert.Contains("Reason: deadline_expired:Too late", serialized);
    }

    [Fact]
    public void Serialize_HeartbeatMessage_MinimalFormat()
    {
        var requestId = Guid.NewGuid();
        var message = new HeartbeatMessage(requestId);
        var serialized = MessageSerializer.Serialize(message);

        Assert.Contains("LNCP/1.0 HEARTBEAT", serialized);
        Assert.Contains($"Request-Id: {requestId}", serialized);
    }

    [Fact]
    public void SerializeToBytes_ProducesUtf8()
    {
        var message = new TextMessage(Guid.NewGuid(), "Bob", "Hello", 1);
        var bytes = MessageSerializer.SerializeToBytes(message);

        Assert.NotEmpty(bytes);
        var text = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.Contains("Hello", text);
    }
}
