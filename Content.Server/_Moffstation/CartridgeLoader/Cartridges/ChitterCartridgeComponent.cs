namespace Content.Server._Moffstation.CartridgeLoader.Cartridges;

[RegisterComponent, Access(typeof(ChitterCartridgeSystem))]
public sealed partial class ChitterCartridgeComponent : Component
{
    public Guid? CurrentChatId;

    /// <summary>
    /// When this cartridge is next allowed to send a message or create a chat.
    /// </summary>
    public TimeSpan NextMessageAllowed;
}
