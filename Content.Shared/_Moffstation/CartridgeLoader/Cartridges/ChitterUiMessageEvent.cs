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
    public ChitterUiMessageType Type { get; set; }
    public Guid? ChatId { get; set; }
    public uint? TargetNumber { get; set; }
    public List<uint>? TargetNumbers { get; set; }
    public string? Content { get; set; }
    public string? ProfilePictureId { get; set; }
    public string? ChatName { get; set; }
}
