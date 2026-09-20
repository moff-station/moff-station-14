namespace Content.Shared._Moffstation.Chitter;

// Common shape for anything that holds per-loader Chitter session state - the PDA cartridge
// (ChitterCartridgeComponent) and the station AI's intrinsic panel (ChitterAiComponent) both need to
// track "which chat am I looking at" and per-action cooldowns, and are otherwise identical. Implementing
// this lets ChitterUiMessageHandler's shared message logic work against either one.
public interface IChitterSession
{
    Guid? CurrentChatId { get; set; }
    TimeSpan NextMessageAllowed { get; set; }
    TimeSpan NextChatAllowed { get; set; }
}
