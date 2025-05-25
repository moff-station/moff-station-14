using Content.Server.StationEvents.Events;
using Content.Shared.Storage;

namespace Content.Server.StationEvents.Components;

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
    /// The number of players per spawn that occurs.
    /// </summary>
    [DataField]
    public int PlayerRatio = 10;

    /// <summary>
    /// The chance per spawn that an additional critter will be spawned (spawns can stack)
    /// </summary>
    [DataField]
    public float ExtraSpawnChance = 0.5f;
}
