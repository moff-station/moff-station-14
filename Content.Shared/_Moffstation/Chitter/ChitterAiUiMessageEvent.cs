using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Chitter;

// Same shape as ChitterUiMessageEvent, but a plain BoundUserInterfaceMessage instead of a CartridgeMessageEvent -
// the AI's Chitter panel is a normal intrinsic UI, not hosted in a cartridge loader.
[Serializable, NetSerializable]
public sealed class ChitterAiUiMessageEvent : BoundUserInterfaceMessage, IChitterUiMessage
{
    public ChitterUiMessageType Type { get; set; }
    public Guid? ChatId { get; set; }
    public uint? TargetNumber { get; set; }
    public List<uint>? TargetNumbers { get; set; }
    public string? Content { get; set; }
    public string? ProfilePictureId { get; set; }
    public string? ChatName { get; set; }
}
