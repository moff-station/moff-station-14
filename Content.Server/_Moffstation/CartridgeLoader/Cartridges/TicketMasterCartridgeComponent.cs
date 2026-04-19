using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Moffstation.CartridgeLoader.Cartridges;

/// <summary>
/// This is used to indicate that a cartridge contain the TicketMaster program.
/// </summary>
[RegisterComponent]
public sealed partial class TicketMasterCartridgeComponent : Component
{
    /// <summary>
    /// Minimum delay between two prints
    /// </summary>
    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(5f);

    /// <summary>
    /// The sound that's played when a ticket is printed.
    /// </summary>
    [DataField("soundPrint")]
    public SoundSpecifier SoundPrint = new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg");

    /// <summary>
    /// What the machine will print
    /// </summary>
    [DataField("machineOutput", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string MachineOutput = "InfractionTicketPaper";

    public TimeSpan NextAvailablePrint =  TimeSpan.Zero;
}
