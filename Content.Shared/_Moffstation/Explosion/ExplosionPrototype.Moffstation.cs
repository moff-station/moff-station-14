using Content.Shared._Moffstation.Explosion;

namespace Content.Shared.Explosion;

public sealed partial class ExplosionPrototype
{
    /// <summary>
    ///     Spawned on clients when an explosion of this type happens. Set to null to spawn nothing.
    /// </summary>
    [DataField]
    public ExplosionEffects? Effects = new();
}
