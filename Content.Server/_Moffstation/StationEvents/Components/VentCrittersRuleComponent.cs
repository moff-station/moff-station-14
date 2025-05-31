using Content.Server._Moffstation.StationEvents.Events;
using Content.Server.StationEvents.Events;
using Content.Shared.Storage;
using Robust.Shared.Map;

namespace Content.Server._Moffstation.StationEvents.Components;

[RegisterComponent, Access(typeof(VentCrittersRule))]
public sealed partial class VentCrittersRuleComponent : Component
{
    [DataField("entries")]
    public List<EntitySpawnEntry> Entries = new();

    /// <summary>
    /// At least one special entry is guaranteed to spawn
    /// </summary>
    [DataField("specialEntries")]
    public List<EntitySpawnEntry> SpecialEntries = new();

    /// <summary>
    /// The amount of chances something gets to spawn. estimated number of spawns can be calculated with (SpawnChances * entryProb)
    /// </summary>
    [DataField]
    public int SpawnAttempts = 100;

    public EntityCoordinates? Location;
}
