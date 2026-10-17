using Content.Shared.Dragon;
using Robust.Shared.Prototypes;

namespace Content.Server.Dragon;

// TODO: replace accumulators with timespan logic
[RegisterComponent]
public sealed partial class DragonRiftComponent : SharedDragonRiftComponent
{
    /// <summary>
    /// Dragon that spawned this rift.
    /// </summary>
    [DataField]
    public EntityUid? Dragon;

    /// <summary>
    /// How long the rift has been active.
    /// </summary>
    [DataField]
    public float Accumulator = 0f;

    /// <summary>
    /// The maximum amount we can accumulate before becoming impervious.
    /// </summary>
    [DataField("maxAccumualator")] // load bearing typo...
    public float MaxAccumulator = 300f;

    /// <summary>
    /// Accumulation of the spawn timer.
    /// </summary>
    [DataField]
    public float SpawnAccumulator = 30f;

    /// <summary>
    /// How long it takes for a new spawn to be added.
    /// </summary>
    [DataField]
    public float SpawnCooldown = 30f;

    [DataField("spawn")]
    public EntProtoId SpawnPrototype = "MobCarpDragon";

    // Moffstation - Start - Empowered carp spawns
    ///  Accumulator for empowerment to track which spawns are replaced. Starts at n-1 for cooldown to ensure spawn on first valid cycle.
    [ViewVariables(VVAccess.ReadWrite)]
    public int EmpoweredSpawnAccumulator = 2;

    /// Frequency for which spawns are empowered.
    [DataField]
    public int EmpoweredSpawnCooldown = 3;

    /// Prototype for empowered spawn.
    [DataField]
    public EntProtoId EmpoweredSpawnPrototype = "MobSharkDragon";
    // Moffstation - End
}
