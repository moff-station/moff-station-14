using Content.Shared._Moffstation.CartridgeLoader.Cartridges;

namespace Content.Shared._Moffstation.Chitter;

// Common shape shared by ChitterUiMessageEvent (cartridge) and ChitterAiUiMessageEvent (AI) - same
// fields, different envelope (CartridgeMessageEvent vs a plain BoundUserInterfaceMessage). Lets
// ChitterUiMessageHandler's shared message logic work against either one.
public interface IChitterUiMessage
{
    ChitterUiMessageType Type { get; }
    Guid? ChatId { get; }
    uint? TargetNumber { get; }
    List<uint>? TargetNumbers { get; }
    string? Content { get; }
    string? ProfilePictureId { get; }
    string? ChatName { get; }
}
