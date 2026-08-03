using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._Moffstation.Medical.CrewMonitoring;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class LongRangeCrewMonitorComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? TargetGrid;
}
