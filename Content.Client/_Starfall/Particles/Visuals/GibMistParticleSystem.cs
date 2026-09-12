using Content.Shared._Starfall.Particles;
using Robust.Shared.Prototypes;

namespace Content.Client._Starfall.Particles;

/// <summary>
/// Receives <see cref="GibMistParticleEvent"/> from the server and spawns
/// a blood-mist particle burst tinted to the entity's actual blood color.
/// </summary>
public sealed partial class GibMistParticleSystem : EntitySystem
{
    [Dependency] private ParticleSystem _particles = default!;

    private static readonly ProtoId<ParticleEffectPrototype> MistEffect = "SfGibMist";
    private static readonly ProtoId<ParticleEffectPrototype> SpeckEffect = "SfGibSpecks";

    [SubscribeNetworkEvent]
    private void OnGibMist(GibMistParticleEvent ev)
    {
        SpawnTinted(MistEffect, ev);
        SpawnTinted(SpeckEffect, ev);
    }

    private void SpawnTinted(ProtoId<ParticleEffectPrototype> effect, GibMistParticleEvent ev)
    {
        var emitter = _particles.SpawnEffect(effect, ev.Coords);
        if (emitter == null)
            return;

        emitter.ColorOverride = ev.BloodColor;
    }
}

