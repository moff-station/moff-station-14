using Content.Server.CartridgeLoader.Cartridges;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Moffstation.LogProbe;

/// <summary>
/// This is used for items that act as a log probe but are not PDA
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class LogProbeComponent : BaseLogProbeComponent
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public override TimeSpan NextPrintAllowed { get; set; } = TimeSpan.FromSeconds(0);
}
