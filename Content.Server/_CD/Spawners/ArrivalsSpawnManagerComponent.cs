using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._CD.Spawners;

/// <summary>
/// This is used for...
/// </summary>
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
