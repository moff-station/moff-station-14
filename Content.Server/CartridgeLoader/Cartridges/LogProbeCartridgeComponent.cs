using Content.Shared._CD.CartridgeLoader.Cartridges; // CD
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.CartridgeLoader.Cartridges;

[Access(typeof(LogProbeCartridgeSystem))]
public abstract partial class BaseLogProbeComponent : Component // Moffstation - Split the component to be reusable
{
    /// <summary>
    /// The name of the scanned entity, sent to clients when they open the UI.
    /// </summary>
    [DataField]
    public string EntityName = string.Empty;

    /// <summary>
    /// The list of pulled access logs
    /// </summary>
    [DataField, ViewVariables]
    public List<PulledAccessLog> PulledAccessLogs = new();

    /// <summary>
    /// The sound to make when we scan something with access
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier SoundScan = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg", AudioParams.Default.WithVariation(0.25f));

    /// <summary>
    /// Paper to spawn when printing logs.
    /// </summary>
    [DataField]
    public EntProtoId<PaperComponent> PaperPrototype = "PaperAccessLogs";

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");

    /// <summary>
    /// How long you have to wait before printing logs again.
    /// </summary>
    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);

    // Moffstation - Begin - Split the component to be reusable

    /// <summary>
    /// When anyone is allowed to spawn another printout.
    /// </summary>
    /// <remarks>
    /// This is abstract as it need to be implemented concretely on component
    /// to put the auto pause attribute.
    /// </remarks>
    public abstract TimeSpan NextPrintAllowed { get; set; }

    // Moffstation - End

    /// <summary>
    /// CD: The last scanned NanoChat data, if any
    /// </summary>
    [DataField]
    public NanoChatData? ScannedNanoChatData;
}

// Moffstation - Begin - Split component
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class LogProbeCartridgeComponent : BaseLogProbeComponent
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public override TimeSpan NextPrintAllowed { get; set; } = TimeSpan.FromSeconds(0);
}
// Moffstation - End
