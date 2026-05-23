using Content.Shared._Moffstation.Abilities.Components;
using Content.Shared._Moffstation.Abilities.Events;
using Content.Shared._Moffstation.Overlay.Components;
using Content.Shared._Moffstation.Overlay.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Movement.Systems;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Abilities.EntitySystems;

/// <summary>
/// This handles the sonic boom ability when it is attached to and activated by an entity with the SonicBoomComponent.
/// </summary>
public sealed class AbilitySonicBoomSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityManager _manager = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly MovementModStatusSystem _move = default!;
    [Dependency] private readonly SharedShockwaveSystem _shockwave = default!;

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

        // Trigger the shockwave
        if (TryComp<ShockwaveComponent>(entity, out var comp))
            _shockwave.Activate((entity, comp));

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
