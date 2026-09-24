using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Explosion;

/// <summary>
///     Client-side cosmetic entities spawned at an explosion's epicenter.
/// </summary>
[DataDefinition]
public sealed partial class ExplosionEffects
{
    [DataField]
    public List<EntProtoId> VisualEffects = new()
    {
        "ExplosionEffectGrenade",
        "ExplosionEffectGrenadeShockWave",
        "ExplosionEffectGrenadeSmoke",
        "ExplosionEffectGrenadeFire",
        "ExplosionEffectGrenadeEmbers",
        "ExplosionEffectGrenadeGlowingEmbers",
    };

    [DataField]
    public List<EntProtoId> ShrapnelEffects = new() { "ExplosionEffectShrapnel1", "ExplosionEffectShrapnel2" };

    /// <summary>
    ///     How many of each shrapnel effect to spawn.
    /// </summary>
    [DataField]
    public MinMax ShrapnelCount = new(5, 9);

    /// <summary>
    ///     Shrapnel speed in tiles per second.
    /// </summary>
    [DataField]
    public float ShrapnelSpeed = 5f;
}
