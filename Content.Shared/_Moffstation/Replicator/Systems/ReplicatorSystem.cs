using System.Linq;
using Content.Shared._Moffstation.Extensions;
using Content.Shared._Moffstation.Replicator.Components;
using Content.Shared.Actions;
using Content.Shared.CombatMode;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Pinpointer;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Replicator.Systems;

/// This system implements the behavior of <see cref="ReplicatorComponent"/>, mostly dealing with upgrading between
/// forms.
public sealed partial class ReplicatorSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MovementModStatusSystem _movementMod = default!;
    [Dependency] private SharedPinpointerSystem _pinpointer = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<CombatModeComponent> _combatModeQuery;
    [Dependency] private EntityQuery<ReplicatorComponent> _replicatorQuery;
    [Dependency] private EntityQuery<ReplicatorNestComponent> _replicatorNestQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplicatorComponent, ReplicatorUpgradeActionEvent>(OnReplicatorUpgradeAction);
        SubscribeLocalEvent<ReplicatorComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<ReplicatorComponent, ToggleCombatActionEvent>(UpdateVisuals,
            after: [typeof(SharedCombatModeSystem)]);
        SubscribeLocalEvent<ReplicatorComponent, MobStateChangedEvent>(UpdateVisuals);
        SubscribeLocalEvent<ReplicatorComponent, EnableReplicatorUpgradesEvent>(OnEnableReplicatorUpgrades);
        SubscribeLocalEvent<ReplicatorComponent, NestDestroyedEvent>(OnNestDestroyed);
        SubscribeLocalEvent<ReplicatorComponent, QueenKilledEvent>(OnQueenKilled);
        SubscribeLocalEvent<ReplicatorComponent, HiveLeaderAssignedEvent>(OnLeaderAssigned);
    }

    private void OnLeaderAssigned(Entity<ReplicatorComponent> entity, ref HiveLeaderAssignedEvent args)
    {
        entity.Comp.HiveLeader = args.Leader;
        SetPinpointerTarget(entity, args.Leader);

        if (entity.Comp.NewLeaderPopup is { } popup)
        {
            _popup.PopupEntity(Loc.GetString(popup), entity, entity, PopupType.Large);
        }
    }

    private void OnNestDestroyed(Entity<ReplicatorComponent> entity, ref NestDestroyedEvent args)
    {
        var subject = entity;
        if (entity.Comp.UpgradeToOnNestDestruction is { } upgrade)
        {
            subject = UpgradeReplicator(entity, upgrade);
        }

        _movementMod.TryUpdateMovementSpeedModDuration(
            subject,
            "HoleDestroyedSlowdownStatusEffect",
            TimeSpan.FromSeconds(3),
            0.8f
        );

        // Clear the pointer to the nest.
        SetPinpointerTarget(subject, null);

        if (entity.Comp.NestDestroyedPopup is { } popup)
        {
            _popup.PopupEntity(
                Loc.GetString(popup),
                subject,
                PopupType.LargeCaution
            );
        }
    }

    private void OnQueenKilled(Entity<ReplicatorComponent> entity, ref QueenKilledEvent args)
    {
        // Clear the pointer to the queen.
        SetPinpointerTarget(entity, null);

        if (entity.Comp.QueenKilledPopup is { } popup)
        {
            _popup.PopupEntity(
                Loc.GetString(popup),
                entity,
                PopupType.LargeCaution
            );
        }
    }

    /// Upgrades <paramref name="ent"/> to the replicator prototype <paramref name="upgradeTo"/>, returning the new
    /// replicator. Any mind in the old replicator is transferred to the new one, and the old replicator is deleted.
    [PublicAPI]
    public Entity<ReplicatorComponent> UpgradeReplicator(
        Entity<ReplicatorComponent> ent,
        EntProtoId<ReplicatorComponent> upgradeTo
    )
    {
        var upgraded = EntityManager.PredictedSpawnAtPosition<ReplicatorComponent>(
            upgradeTo,
            Transform(ent).Coordinates
        );

        if (ent.Comp.HiveLeader is { } nest)
        {
            var nestEvent = new MemberReplicatorUpgradedEvent(ent, upgraded);
            RaiseLocalEvent(nest, ref nestEvent);

            SetPinpointerTarget(upgraded, nest);
        }

        if (_mind.TryGetMind(ent, out var mindEnt, out var mindComp))
        {
            _mind.TransferTo(mindEnt, upgraded, mind: mindComp);
        }

        _audio.PlayPredicted(upgraded.Comp.UpgradeSound, upgraded, upgraded);
        _popup.PopupPredicted(
            Loc.GetStringOrNull(upgraded.Comp.UpgradedPopupSelf),
            Loc.GetStringOrNull(upgraded.Comp.UpgradedPopupOther),
            upgraded,
            upgraded,
            PopupType.MediumCaution
        );

        PredictedQueueDel(ent);

        return upgraded;
    }


    private void OnAttackAttempt(Entity<ReplicatorComponent> ent, ref AttackAttemptEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        // Can't attack your friends.
        if (_replicatorQuery.HasComp(args.Target))
        {
            _popup.PopupClient(Loc.GetString("replicator-on-replicator-attack-fail"), ent, PopupType.MediumCaution);
            args.Cancel();
        }

        // Can't attack the nest.
        if (_replicatorNestQuery.HasComp(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("replicator-on-nest-attack-fail"), ent, ent, PopupType.MediumCaution);
            args.Cancel();
        }
    }

    private void UpdateVisuals<TEvent>(Entity<ReplicatorComponent> ent, ref TEvent args)
    {
        // turn on combat visuals if the mob is alive and in combat mode. otherwise turn them off
        var inCombatMode = _combatModeQuery.CompOrNull(ent)?.IsInCombatMode ?? false;
        _appearance.SetData(ent, ReplicatorVisuals.Combat, _mobState.IsAlive(ent) && inCombatMode);
    }

    private void OnEnableReplicatorUpgrades(Entity<ReplicatorComponent> entity, ref EnableReplicatorUpgradesEvent args)
    {
        var actions = entity.Comp.UpgradeOptionActions
            .Select(actionId => _actions.AddAction(entity, actionId))
            .OfType<EntityUid>();
        entity.Comp.UpgradeActionEntities.AddRange(actions);
    }

    private void OnReplicatorUpgradeAction(Entity<ReplicatorComponent> ent, ref ReplicatorUpgradeActionEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        UpgradeReplicator(ent, args.NextStage);
    }

    private void SetPinpointerTarget(Entity<ReplicatorComponent> entity, EntityUid? target)
    {
        if (!_inventory.TryGetContainerSlotEnumerator(entity.Owner, out var inventory))
            return;

        while (inventory.NextItem(out var item))
        {
            _pinpointer.SetTarget(item, target);
        }
    }
}
