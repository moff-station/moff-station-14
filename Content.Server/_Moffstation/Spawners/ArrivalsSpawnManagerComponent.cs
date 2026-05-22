using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Spawners;

/// This component is used to track state related to player spawning, enabling players to spawn either at arrivals or on the station at round start.
[RegisterComponent]
public sealed partial class ArrivalsSpawnManagerComponent : Component
{
    [DataField]
    public float StationSpawnChance = 0.6f;

    [DataField]
    public int StationSpawnMaxLimit = 5;

    [DataField]
    public int StationSpawnLimit;

    [DataField]
    public int StationSpawnCount;

    [ViewVariables]
    public ProtoId<AntagPrototype> OpeningShiftProto= "OpeningShift";
}
