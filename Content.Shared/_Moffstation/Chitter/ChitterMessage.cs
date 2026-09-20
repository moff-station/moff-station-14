using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Chitter;

[Serializable, NetSerializable]
public sealed class ChitterAccount
{
    public uint AccountId;
    public string Name = string.Empty;
    public string JobTitle = string.Empty;
    public string ProfilePictureId = string.Empty;
}

[Serializable, NetSerializable]
public sealed class ChitterMessage
{
    public Guid MessageId;
    public uint SenderAccountId;
    public string SenderName = string.Empty;
    public TimeSpan Timestamp;
    public string Content = string.Empty;
    public bool DeliveryFailed;
}

// Recorded alongside each message so conversations can be pulled from a saved replay later.
[Serializable, NetSerializable]
public sealed class ChitterReplayMessageRecord
{
    public Guid ChatId;
    public ChitterMessage Message = new();
}

[Serializable, NetSerializable]
public sealed class ChitterChat
{
    public Guid ChatId;
    public string ChatName = string.Empty;
    public List<uint> ParticipantAccountIds = new();
    public List<ChitterMessage> Messages = new();
    public TimeSpan CreatedTime;
    public bool Archived;

    // How many messages each participating account has seen, keyed by AccountId so read state
    // follows the account instead of whichever PDA cartridge last rendered it.
    public Dictionary<uint, int> LastSeenMessageCount = new();
}
