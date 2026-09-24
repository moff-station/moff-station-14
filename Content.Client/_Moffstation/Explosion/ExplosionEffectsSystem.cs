using Content.Client._Starfall.Particles;
using Content.Shared._Moffstation.Explosion.Components;
using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using Robust.Client.Graphics;
using Robust.Shared.Timing;

namespace Content.Client._Moffstation.Explosion;

/// <summary>
///     Plays an explosion type's <see cref="ExplosionPrototype.Effects"/>.
/// </summary>
public sealed partial class ExplosionEffectsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private ParticleSystem _particles = default!;
    [Dependency] private SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new ExplosionShockWaveOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<ExplosionShockWaveOverlay>();
    }

    [SubscribeLocalEvent]
    private void OnExplosionStartup(Entity<ExplosionVisualsComponent> ent, ref ComponentStartup args)
    {
        var epicenter = ent.Comp.Epicenter;
        if (!_map.MapExists(epicenter.MapId) ||
            !ProtoMan.Resolve<ExplosionPrototype>(ent.Comp.ExplosionType, out var type) ||
            type.Effects is not { } effects)
            return;

        foreach (var particle in effects.Particles)
        {
            _particles.CreateParticle(particle, epicenter);
        }

        foreach (var entity in effects.Entities)
        {
            Spawn(entity, epicenter);
        }
    }

    [SubscribeLocalEvent]
    private void OnShockWaveStartup(Entity<ExplosionShockWaveComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.StartTime = _timing.RealTime;
    }
}
