using Content.Shared._Moffstation.Chitter;

namespace Content.Server._Moffstation.CartridgeLoader.Cartridges;

[RegisterComponent, Access(typeof(ChitterCartridgeSystem))]
public sealed partial class ChitterCartridgeComponent : Component, IChitterSession
{
    public Guid? CurrentChatId { get; set; }

    // Separate cooldowns so a recent message send can't make a chat-creation request get
    // silently dropped (the client closes the New Chat dialog either way, success or not).
    public TimeSpan NextMessageAllowed { get; set; }
    public TimeSpan NextChatAllowed { get; set; }
}
