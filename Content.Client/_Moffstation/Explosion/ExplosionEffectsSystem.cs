using Content.Shared._Moffstation.Explosion.Components;
using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using Content.Shared.Throwing;
using Robust.Client.Graphics;
using Robust.Client.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._Moffstation.Explosion;

/// <summary>
///     Spawns an explosion type's <see cref="ExplosionPrototype.Effects"/> when its visuals reach the client.
/// </summary>
public sealed partial class ExplosionEffectsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    private const float ShrapnelThrowDistance = 10f;

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

        foreach (var effect in effects.VisualEffects)
        {
            Spawn(effect, epicenter);
        }

        foreach (var effect in effects.ShrapnelEffects)
        {
            var count = effects.ShrapnelCount.Next(_random);
            for (var i = 0; i < count; i++)
            {
                var shrapnel = Spawn(effect, epicenter);
                _physics.UpdateIsPredicted(shrapnel);
                _throwing.TryThrow(shrapnel, _random.NextAngle().ToVec() * ShrapnelThrowDistance, effects.ShrapnelSpeed);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnShockWaveStartup(Entity<ExplosionShockWaveComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.StartTime = _timing.RealTime;
    }

    [SubscribeLocalEvent]
    private void OnShrapnelUpdateIsPredicted(Entity<ExplosionShrapnelComponent> ent, ref UpdateIsPredictedEvent args)
    {
        args.IsPredicted = true;
    }

    [SubscribeLocalEvent]
    private void OnShrapnelStartCollide(Entity<ExplosionShrapnelComponent> ent, ref StartCollideEvent args)
    {
        QueueDel(ent);
    }
}
