using Content.Shared._Starfall.Particles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Explosion;

/// <summary>
///     Client-side cosmetic effects played at an explosion's epicenter.
/// </summary>
[DataDefinition]
public sealed partial class ExplosionEffects
{
    [DataField]
    public List<ProtoId<ParticleEffectPrototype>> Particles = new()
    {
        "ExplosionFlash",
        "ExplosionSmoke",
        "ExplosionFire",
        "ExplosionEmbers",
        "ExplosionGlowingEmbers",
    };

    /// <summary>
    ///     Entities for effects particles can't do, like lights and the shockwave.
    /// </summary>
    [DataField]
    public List<EntProtoId> Entities = new() { "ExplosionEffectLight", "ExplosionEffectShockWave" };
}
