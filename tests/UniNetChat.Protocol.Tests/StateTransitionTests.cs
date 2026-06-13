using UniNetChat.Protocol.States;
using Xunit;

namespace UniNetChat.Protocol.Tests;

public class StateTransitionTests
{
    [Theory]
    [InlineData(InitiatorState.Idle, InitiatorState.Discovering, true)]
    [InlineData(InitiatorState.Discovering, InitiatorState.WaitingConnection, true)]
    [InlineData(InitiatorState.WaitingConnection, InitiatorState.Handshaking, true)]
    [InlineData(InitiatorState.Handshaking, InitiatorState.Connected, true)]
    [InlineData(InitiatorState.Connected, InitiatorState.Closing, true)]
    [InlineData(InitiatorState.Closing, InitiatorState.Closed, true)]
    [InlineData(InitiatorState.Idle, InitiatorState.Connected, false)]
    [InlineData(InitiatorState.Connected, InitiatorState.Discovering, false)]
    [InlineData(InitiatorState.Closed, InitiatorState.Connected, false)]
    public void InitiatorTransition_ValidatesCorrectly(InitiatorState from, InitiatorState to, bool expected)
    {
        var result = StateTransitions.IsValidTransition(from, to);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(RecipientState.Listening, RecipientState.Connecting, true)]
    [InlineData(RecipientState.Connecting, RecipientState.Handshaking, true)]
    [InlineData(RecipientState.Handshaking, RecipientState.Connected, true)]
    [InlineData(RecipientState.Connected, RecipientState.Closing, true)]
    [InlineData(RecipientState.Closing, RecipientState.Closed, true)]
    [InlineData(RecipientState.Closed, RecipientState.Listening, true)]
    [InlineData(RecipientState.Listening, RecipientState.Connected, false)]
    [InlineData(RecipientState.Connected, RecipientState.Listening, false)]
    public void RecipientTransition_ValidatesCorrectly(RecipientState from, RecipientState to, bool expected)
    {
        var result = StateTransitions.IsValidTransition(from, to);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void InitiatorConnected_CanSendMessages()
    {
        var validMessages = StateTransitions.GetValidOutgoingMessages(InitiatorState.Connected);

        Assert.Contains(Messages.MessageType.Msg, validMessages);
        Assert.Contains(Messages.MessageType.Ack, validMessages);
        Assert.Contains(Messages.MessageType.Close, validMessages);
    }

    [Fact]
    public void InitiatorIdle_CannotSendAnyMessages()
    {
        var validMessages = StateTransitions.GetValidOutgoingMessages(InitiatorState.Idle);

        Assert.Empty(validMessages);
    }

    [Fact]
    public void RecipientListening_CanReceiveDiscover()
    {
        var validMessages = StateTransitions.GetValidIncomingMessages(RecipientState.Listening);

        Assert.Contains(Messages.MessageType.Discover, validMessages);
    }

    [Fact]
    public void RecipientHandshaking_CanReceiveAcceptOrReject()
    {
        var validMessages = StateTransitions.GetValidIncomingMessages(RecipientState.Handshaking);

        Assert.Contains(Messages.MessageType.Accept, validMessages);
        Assert.Contains(Messages.MessageType.Reject, validMessages);
    }
}
