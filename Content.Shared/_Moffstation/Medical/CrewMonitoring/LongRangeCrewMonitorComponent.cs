using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._Moffstation.Medical.CrewMonitoring;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class LongRangeCrewMonitorComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? TargetGrid { get; set; }
}
