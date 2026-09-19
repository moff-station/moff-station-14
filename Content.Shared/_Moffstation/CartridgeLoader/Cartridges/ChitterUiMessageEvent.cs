using Content.Shared._Moffstation.Chitter;
using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public enum ChitterUiMessageType
{
    NewChat,
    SelectChat,
    SendMessage,
    LeaveChat,
    AddParticipant,
    RemoveParticipant,
    ArchiveChat,
    SetProfilePicture,
    RefreshContacts,
    RenameChat,
    BlockContact,
    UnblockContact,
}

[Serializable, NetSerializable]
public sealed class ChitterUiMessageEvent : CartridgeMessageEvent, IChitterUiMessage
{
    public ChitterUiMessageType Type { get; }
    public Guid? ChatId { get; }
    public uint? TargetNumber { get; }
    public List<uint>? TargetNumbers { get; }
    public string? Content { get; }
    public string? ProfilePictureId { get; }
    public string? ChatName { get; }

    public ChitterUiMessageEvent(
        ChitterUiMessageType type,
        Guid? chatId,
        uint? targetNumber,
        List<uint>? targetNumbers,
        string? content,
        string? profilePictureId,
        string? chatName)
    {
        Type = type;
        ChatId = chatId;
        TargetNumber = targetNumber;
        TargetNumbers = targetNumbers;
        Content = content;
        ProfilePictureId = profilePictureId;
        ChatName = chatName;
    }
}
