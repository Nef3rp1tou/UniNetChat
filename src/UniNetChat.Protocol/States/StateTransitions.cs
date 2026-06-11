using UniNetChat.Protocol.Messages;

namespace UniNetChat.Protocol.States;

/// <summary>
/// Manages state transitions for LNCP connections.
/// </summary>
public static class StateTransitions
{
    /// <summary>
    /// Gets the valid transitions for an initiator state.
    /// </summary>
    public static InitiatorState[] GetValidTransitions(InitiatorState current)
    {
        return current switch
        {
            InitiatorState.Idle => [InitiatorState.Discovering],
            InitiatorState.Discovering => [InitiatorState.WaitingConnection, InitiatorState.Idle],
            InitiatorState.WaitingConnection => [InitiatorState.Handshaking, InitiatorState.Idle],
            InitiatorState.Handshaking => [InitiatorState.Connected, InitiatorState.Closed],
            InitiatorState.Connected => [InitiatorState.Closing, InitiatorState.Closed],
            InitiatorState.Closing => [InitiatorState.Closed],
            InitiatorState.Closed => [],
            _ => []
        };
    }

    /// <summary>
    /// Gets the valid transitions for a recipient state.
    /// </summary>
    public static RecipientState[] GetValidTransitions(RecipientState current)
    {
        return current switch
        {
            RecipientState.Listening => [RecipientState.Connecting],
            RecipientState.Connecting => [RecipientState.Handshaking, RecipientState.Listening],
            RecipientState.Handshaking => [RecipientState.Connected, RecipientState.Closed],
            RecipientState.Connected => [RecipientState.Closing, RecipientState.Closed],
            RecipientState.Closing => [RecipientState.Closed],
            RecipientState.Closed => [RecipientState.Listening],
            _ => []
        };
    }

    /// <summary>
    /// Checks if a transition is valid for the initiator.
    /// </summary>
    public static bool IsValidTransition(InitiatorState from, InitiatorState to)
    {
        return GetValidTransitions(from).Contains(to);
    }

    /// <summary>
    /// Checks if a transition is valid for the recipient.
    /// </summary>
    public static bool IsValidTransition(RecipientState from, RecipientState to)
    {
        return GetValidTransitions(from).Contains(to);
    }

    /// <summary>
    /// Gets the valid message types that can be sent in a given initiator state.
    /// </summary>
    public static MessageType[] GetValidOutgoingMessages(InitiatorState state)
    {
        return state switch
        {
            InitiatorState.Idle => [],
            InitiatorState.Discovering => [MessageType.Discover],
            InitiatorState.WaitingConnection => [],
            InitiatorState.Handshaking => [MessageType.Accept, MessageType.Reject],
            InitiatorState.Connected => [MessageType.Msg, MessageType.Ack, MessageType.Close],
            InitiatorState.Closing => [MessageType.Closed],
            InitiatorState.Closed => [],
            _ => []
        };
    }

    /// <summary>
    /// Gets the valid message types that can be sent in a given recipient state.
    /// </summary>
    public static MessageType[] GetValidOutgoingMessages(RecipientState state)
    {
        return state switch
        {
            RecipientState.Listening => [],
            RecipientState.Connecting => [MessageType.Connect],
            RecipientState.Handshaking => [],
            RecipientState.Connected => [MessageType.Msg, MessageType.Ack, MessageType.Close],
            RecipientState.Closing => [MessageType.Closed],
            RecipientState.Closed => [],
            _ => []
        };
    }

    /// <summary>
    /// Gets the valid message types that can be received in a given initiator state.
    /// </summary>
    public static MessageType[] GetValidIncomingMessages(InitiatorState state)
    {
        return state switch
        {
            InitiatorState.Idle => [],
            InitiatorState.Discovering => [],
            InitiatorState.WaitingConnection => [MessageType.Connect],
            InitiatorState.Handshaking => [],
            InitiatorState.Connected => [MessageType.Msg, MessageType.Ack, MessageType.Close],
            InitiatorState.Closing => [MessageType.Closed],
            InitiatorState.Closed => [],
            _ => []
        };
    }

    /// <summary>
    /// Gets the valid message types that can be received in a given recipient state.
    /// </summary>
    public static MessageType[] GetValidIncomingMessages(RecipientState state)
    {
        return state switch
        {
            RecipientState.Listening => [MessageType.Discover],
            RecipientState.Connecting => [],
            RecipientState.Handshaking => [MessageType.Accept, MessageType.Reject],
            RecipientState.Connected => [MessageType.Msg, MessageType.Ack, MessageType.Close],
            RecipientState.Closing => [MessageType.Closed],
            RecipientState.Closed => [],
            _ => []
        };
    }
}
