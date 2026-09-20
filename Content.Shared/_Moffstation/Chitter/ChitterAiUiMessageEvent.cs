using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Chitter;

// Same shape as ChitterUiMessageEvent, but a plain BoundUserInterfaceMessage instead of a CartridgeMessageEvent -
// the AI's Chitter panel is a normal intrinsic UI, not hosted in a cartridge loader.
[Serializable, NetSerializable]
public sealed class ChitterAiUiMessageEvent : BoundUserInterfaceMessage, IChitterUiMessage
{
    public ChitterUiMessageType Type { get; }
    public Guid? ChatId { get; }
    public uint? TargetNumber { get; }
    public List<uint>? TargetNumbers { get; }
    public string? Content { get; }
    public string? ProfilePictureId { get; }
    public string? ChatName { get; }

    public ChitterAiUiMessageEvent(
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
