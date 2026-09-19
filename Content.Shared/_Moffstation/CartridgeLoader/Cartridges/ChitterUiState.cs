using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class ChitterUiState : BoundUserInterfaceState
{
    public List<ChatEntry> Chats = new();
    public ChatDetail? CurrentChat;
    public List<AccountEntry> Contacts = new();
    public List<AccountEntry> BlockedContacts = new();
    public uint OwnNumber;
    public string OwnName = string.Empty;
    public string OwnJob = string.Empty;
    public string OwnProfilePicture = string.Empty;
    public bool ServerOnline;
    public bool HasIdCard;
}

[Serializable, NetSerializable]
public sealed class ChatEntry
{
    public Guid ChatId;
    public string DisplayName = string.Empty;
    public string LastMessage = string.Empty;
    public bool HasUnread;
    public int UnreadCount;
}

[Serializable, NetSerializable]
public sealed class ChatDetail
{
    public Guid ChatId;
    public string ChatName = string.Empty;
    public List<MessageEntry> Messages = new();
    public List<ParticipantEntry> Participants = new();
}

[Serializable, NetSerializable]
public sealed class MessageEntry
{
    public Guid MessageId;
    public uint SenderId;
    public string SenderName = string.Empty;
    public string SenderProfilePicture = string.Empty;
    public TimeSpan Timestamp;
    public string Content = string.Empty;
    public bool DeliveryFailed;
    public bool IsOwn;
    public bool IsNew;

    // Profile picture ids of every other participant who has viewed at least this many messages -
    // lets the client show a row of small "seen by" avatars the same way most chat apps do.
    public List<string> SeenByProfilePictures = new();
}

[Serializable, NetSerializable]
public sealed class ParticipantEntry
{
    public uint AccountId;
    public string Name = string.Empty;
    public string JobTitle = string.Empty;
    public string ProfilePictureId = string.Empty;
}

[Serializable, NetSerializable]
public sealed class AccountEntry
{
    public uint AccountId;
    public string Name = string.Empty;
    public string JobTitle = string.Empty;
    public string ProfilePictureId = string.Empty;
}
