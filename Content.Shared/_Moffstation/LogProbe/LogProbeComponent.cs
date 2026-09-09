using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Moffstation.LogProbe;

/// <summary>
/// This is used for items that act as a log probe but are not PDA
/// </summary>
[RegisterComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class LogProbeComponent : BaseLogProbeComponent
{
    [DataField, AutoNetworkedField]
    public override string EntityName { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public override List<PulledAccessLog> PulledAccessLogs { get; set; } = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public override TimeSpan NextPrintAllowed { get; set; } = TimeSpan.FromSeconds(0);
}
