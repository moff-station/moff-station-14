using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Chitter;

/// The ID of a <see cref="ChitterChat"/>.
[Serializable, NetSerializable]
public readonly record struct ChitterChatId(Guid Id)
{
    public static ChitterChatId Random() => new(Guid.NewGuid());
}

/// The record of a chitter chat conversation.
[DataRecord, Serializable, NetSerializable]
public partial record struct ChitterChat(
    ChitterChatId Id,
    string Name,
    HashSet<ChitterAccountId> Participants,
    List<ChitterMessage> Messages,
    TimeSpan CreatedTime
);

/// The ID of a <see cref="ChitterMessage"/>.
[Serializable, NetSerializable]
public readonly record struct ChitterMessageId(Guid Id)
{
    public static ChitterMessageId Random() => new(Guid.NewGuid());
}

/// The record of a message in a chitter chat conversation.
[DataRecord, Serializable, NetSerializable]
public partial record struct ChitterMessage(
    ChitterMessageId Id,
    ChitterAccountId Sender,
    TimeSpan Timestamp,
    string Content
);

/// Stores chitter messages for replays.
[Serializable, NetSerializable]
public readonly record struct ChitterMessageReplayRecord(ChitterChatId Chat, ChitterMessage Message);
