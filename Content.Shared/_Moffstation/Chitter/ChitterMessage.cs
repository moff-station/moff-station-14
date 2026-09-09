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

/// <summary>
/// Recorded into the replay stream alongside each Chitter message so conversations can be recovered
/// from a saved replay later (e.g. for report follow-up), independent of the live admin log panel.
/// </summary>
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

    /// <summary>
    /// How many of this chat's messages each participating account has seen. Keyed by AccountId
    /// so read state follows the account rather than whichever PDA cartridge last rendered it.
    /// </summary>
    public Dictionary<uint, int> LastSeenMessageCount = new();
}
