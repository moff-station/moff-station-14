using Content.Shared._Moffstation.Abilities.Components;
using Content.Shared._Moffstation.Abilities.Events;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Movement.Systems;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Abilities.EntitySystems;

/// <summary>
/// This handles the sonic boom ability when it is attached to and activated by an entity with the SonicBoomComponent.
/// </summary>
public sealed partial class AbilitySonicBoomSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityManager _manager = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private MovementModStatusSystem _move = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbilitySonicBoomComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AbilitySonicBoomComponent, AbilitySonicBoomEvent>(OnBoom);
    }

    private void OnMapInit(Entity<AbilitySonicBoomComponent> entity, ref MapInitEvent args)
    {
        _action.AddAction(entity.Owner, ref entity.Comp.Action, entity.Comp.ActionProto, entity.Owner);
    }

    private void OnBoom(Entity<AbilitySonicBoomComponent> entity, ref AbilitySonicBoomEvent args)
    {
        if (args.Handled)
            return;

        var netEntity = _manager.GetNetEntity(entity.Owner);
        _random.SetSeed(netEntity.Id + (int)_timing.CurTick.Value);

        if (!_timing.IsFirstTimePredicted)
            return;

        var entityCoords = _transform.GetMoverCoordinates(entity);

        foreach (var target in _lookup.GetEntitiesInRange(entity, entity.Comp.FlingRadius, LookupFlags.Uncontained))
        {
            var thrownVec = _random.NextVector2(0.05f) +
                            (_transform.GetMoverCoordinates(target).Position - entityCoords.Position);

            _throwing.TryThrow(
                target,
                thrownVec.Normalized() * (entity.Comp.FlingStrength / (1.0f + thrownVec.LengthSquared())),
                pushbackRatio: 0.0f);
        }

        _manager.SpawnAttachedTo(entity.Comp.ShockwaveProto, Transform(entity).Coordinates);
        _audio.PlayPredicted(entity.Comp.Sound, entity.Owner, entity.Owner);

        _move.TryAddMovementSpeedModDuration(
            entity,
            MovementModStatusSystem.FlashSlowdown,
            entity.Comp.SlowdownDuration,
            entity.Comp.Slowdown);

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(entity):user} used the sonic boom ability.");

        args.Handled = true;
    }
}
