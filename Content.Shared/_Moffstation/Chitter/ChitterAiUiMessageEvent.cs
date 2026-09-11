using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Chitter;

// Same shape as ChitterUiMessageEvent, but a plain BoundUserInterfaceMessage instead of a CartridgeMessageEvent -
// the AI's Chitter panel is a normal intrinsic UI, not hosted in a cartridge loader.
[Serializable, NetSerializable]
public sealed class ChitterAiUiMessageEvent : BoundUserInterfaceMessage
{
    public ChitterUiMessageType Type;
    public Guid? ChatId;
    public uint? TargetNumber;
    public List<uint>? TargetNumbers;
    public string? Content;
    public string? ProfilePictureId;
    public string? ChatName;
}
