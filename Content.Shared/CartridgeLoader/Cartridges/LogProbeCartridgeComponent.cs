using Content.Shared._CD.CartridgeLoader.Cartridges;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.CartridgeLoader.Cartridges;

[NetworkedComponent] // Moffstation - Split the component to be reusable
[Access(typeof(LogProbeCartridgeSystem))]
public abstract partial class BaseLogProbeComponent : Component // Moffstation - Split the component to be reusable
{
    /// <summary>
    /// The name of the scanned entity, sent to clients when they open the UI.
    /// </summary>
    [DataField] // Moffstation - abstract to force putting `AutoNetworkedField` on the inheritor
    public abstract string EntityName { get; set; } // Moffstation - abstract to force putting `AutoNetworkedField` on the inheritor

    /// <summary>
    /// The list of pulled access logs
    /// </summary>
    [DataField] // Moffstation - abstract to force putting `AutoNetworkedField` on the inheritor
    public abstract List<PulledAccessLog> PulledAccessLogs { get; set; } // Moffstation - abstract to force putting `AutoNetworkedField` on the inheritor

    /// <summary>
    /// The sound to make when we scan something with access
    /// </summary>
    [DataField]
    public SoundSpecifier SoundScan =
        new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg", AudioParams.Default.WithVariation(0.25f));

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
    /// This is abstract as it needs to be implemented concretely on the inheriting component with
    /// <see cref="AutoPausedFieldAttribute"/>.
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
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class LogProbeCartridgeComponent : BaseLogProbeComponent
{
    [DataField, AutoNetworkedField]
    public override string EntityName { get; set; }= string.Empty;
    [DataField, AutoNetworkedField]
    public override List<PulledAccessLog> PulledAccessLogs { get; set; } = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public override TimeSpan NextPrintAllowed { get; set; } = TimeSpan.Zero;
}
// Moffstation - End
