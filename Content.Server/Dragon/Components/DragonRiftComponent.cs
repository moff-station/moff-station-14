using Content.Shared.Dragon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Dragon;

[RegisterComponent]
public sealed partial class DragonRiftComponent : SharedDragonRiftComponent
{
    /// <summary>
    /// Dragon that spawned this rift.
    /// </summary>
    [DataField("dragon")] public EntityUid? Dragon;

    /// <summary>
    /// How long the rift has been active.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("accumulator")]
    public float Accumulator = 0f;

    /// <summary>
    /// The maximum amount we can accumulate before becoming impervious.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxAccumuluator")] public float MaxAccumulator = 300f;

    /// <summary>
    /// Accumulation of the spawn timer.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("spawnAccumulator")]
    public float SpawnAccumulator = 30f;

    /// <summary>
    /// How long it takes for a new spawn to be added.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("spawnCooldown")]
    public float SpawnCooldown = 30f;

    [ViewVariables(VVAccess.ReadWrite), DataField("spawn", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnPrototype = "MobCarpDragon";

    // Moffstation - Empowered carp spawns
    /// <summary>
    ///  Accumulator for empowerment to track which spawns are replaced.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("empoweredSpawnAccumulator")]
    public float EmpoweredSpawnAccumulator = 3f;

    /// <summary>
    /// Frequency for which spawns are empowered.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("empoweredSpawnCooldown")]
    public float EmpoweredSpawnCooldown = 3f;

    /// <summary>
    /// Prototype for empowered spawn.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("empoweredSpawn", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string EmpoweredSpawnPrototype = "MobSharkDragon";
}
