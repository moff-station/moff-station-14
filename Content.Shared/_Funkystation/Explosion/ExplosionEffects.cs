using Robust.Shared.Prototypes;

namespace Content.Shared._Funkystation.Explosion;

/// <summary>
///     Visual entities spawned at the epicenter when an explosive triggers.
/// </summary>
[DataDefinition]
public sealed partial class ExplosionEffects
{
    /// <summary>
    ///     A list of entities spawned at the epicenter, all at the same time
    /// </summary>
    [DataField]
    public List<EntProtoId> VisualEffects = new()
    {
        "ExplosionEffectGrenade",
        "ExplosionEffectGrenadeShockWave",
        "ExplosionEffectGrenadeSmoke",
        "ExplosionEffectGrenadeEmbers",
        "ExplosionEffectGrenadeGlowingEmbers"
    };

    [DataField]
    public List<EntProtoId> ShrapnelEffects = new() { "ExplosionEffectShrapnel1", "ExplosionEffectShrapnel2" };

    [DataField]
    public int MinShrapnel = 5;

    [DataField]
    public int MaxShrapnel = 9;

    [DataField]
    public float ShrapnelSpeed = 5f;
}
