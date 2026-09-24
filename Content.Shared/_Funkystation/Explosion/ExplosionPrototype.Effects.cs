using Content.Shared._Funkystation.Explosion;

namespace Content.Shared.Explosion;

public sealed partial class ExplosionPrototype
{
    /// <summary>
    ///     Spawned when an explosive of this type triggers. Set to null to spawn nothing.
    /// </summary>
    [DataField]
    public ExplosionEffects? Effects = new();
}
