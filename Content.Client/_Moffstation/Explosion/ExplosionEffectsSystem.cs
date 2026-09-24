using Content.Client._Starfall.Particles;
using Content.Shared._Moffstation.Explosion.Components;
using Content.Shared._Starfall.Particles;
using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using Content.Shared.Physics;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._Moffstation.Explosion;

/// <summary>
///     Plays an explosion type's <see cref="ExplosionPrototype.Effects"/>.
/// </summary>
public sealed partial class ExplosionEffectsSystem : EntitySystem
{
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private ParticleSystem _particles = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;

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

        // Rays start inside whatever exploded, so don't let those stop the shrapnel.
        var ignored = _lookup.GetEntitiesIntersecting(epicenter);

        foreach (var shrapnel in effects.Shrapnel)
        {
            if (!ProtoMan.Resolve(shrapnel, out var shrapnelProto) || shrapnelProto.Speed <= 0f)
                continue;

            var count = effects.ShrapnelCount.Next(_random);
            for (var i = 0; i < count; i++)
            {
                FireShrapnel(shrapnelProto, epicenter, ignored);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnShockWaveStartup(Entity<ExplosionShockWaveComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.StartTime = _timing.RealTime;
    }

    private void FireShrapnel(ParticleEffectPrototype proto, MapCoordinates epicenter, HashSet<EntityUid> ignored)
    {
        var direction = _random.NextAngle().ToVec();
        var lifetime = (float) proto.Lifetime.TotalSeconds;

        var ray = new CollisionRay(epicenter.Position, direction, (int) CollisionGroup.ItemMask);
        var hits = _physics.IntersectRayWithPredicate(epicenter.MapId,
            ray,
            ignored,
            static (uid, set) => set.Contains(uid),
            lifetime * proto.Speed,
            returnOnFirstHit: false);

        foreach (var hit in hits)
        {
            lifetime = MathF.Min(lifetime, hit.Distance / proto.Speed);
        }

        var screenDirection = _eye.CurrentEye.Rotation.RotateVec(direction);
        var overrides = new ParticleRuntimeOverrides
        {
            EmitAngle = new Angle(MathF.Atan2(screenDirection.X, screenDirection.Y)),
            Lifetime = TimeSpan.FromSeconds(lifetime),
        };

        _particles.CreateParticle(proto.ID, epicenter, overrides: overrides);
    }
}
