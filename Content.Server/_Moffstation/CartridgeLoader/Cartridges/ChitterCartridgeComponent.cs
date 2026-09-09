namespace Content.Server._Moffstation.CartridgeLoader.Cartridges;

[RegisterComponent, Access(typeof(ChitterCartridgeSystem))]
public sealed partial class ChitterCartridgeComponent : Component
{
    public Guid? CurrentChatId;

    /// <summary>
    /// When this cartridge is next allowed to send a message. Independent of
    /// <see cref="NextChatAllowed"/> so a recent send can't make a chat-creation request get
    /// silently dropped (the client closes the New Chat dialog optimistically either way).
    /// </summary>
    public TimeSpan NextMessageAllowed;

    /// <summary>
    /// When this cartridge is next allowed to create a chat.
    /// </summary>
    public TimeSpan NextChatAllowed;
}
