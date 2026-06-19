using Content.Shared._Moffstation.Extensions;
using Content.Shared._Moffstation.Replicator.Components;
using Content.Shared.Actions;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Replicator.Systems;

/// This system implements the behavior of <see cref="ReplicatorQueenComponent"/>, mostly spawning new nests.
public sealed partial class ReplicatorQueenSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private ReplicatorHiveLeaderSystem _hiveLeader = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private ReplicatorSystem _replicator = default!;
    [Dependency] private SharedReplicatorNestSystem _replicatorNest = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<ReplicatorComponent> _replicatorQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplicatorQueenComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ReplicatorQueenComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<ReplicatorQueenComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ReplicatorQueenComponent, ReplicatorSpawnNestActionEvent>(OnSpawnNestAction);

        SubscribeLocalEvent<ReplicatorQueenSignComponent, ComponentStartup>(ReplicatorSignAdded);
        SubscribeLocalEvent<ReplicatorQueenSignComponent, ComponentShutdown>(ReplicatorSignRemoved);
    }

    private void OnMapInit(Entity<ReplicatorQueenComponent> entity, ref MapInitEvent args)
    {
        _actions.AddAction(entity, ref entity.Comp.SpawnNestActionEnt, entity.Comp.SpawnNewNestAction, entity);

        // Ensure we are a member of our own hive.
        if (_replicatorQuery.ResolveOrNull(entity) is { } replicator)
        {
            _hiveLeader.AssignReplicators(entity.Owner, [replicator]);
        }
    }

    private void OnRemove(Entity<ReplicatorQueenComponent> ent, ref ComponentRemove args)
    {
        RemCompDeferred<ReplicatorHiveLeaderComponent>(ent);
        RemCompDeferred<ReplicatorQueenSignComponent>(ent);
    }

    private void OnMobStateChanged(Entity<ReplicatorQueenComponent> ent, ref MobStateChangedEvent args)
    {
        if (_mobState.IsAlive(ent))
            return;

        // Killing the queen makes it stop being the queen.
        RemCompDeferred<ReplicatorQueenComponent>(ent);

        // Notify related replicators that they're orphaned.
        var ev = new QueenKilledEvent();
        foreach (var member in _hiveLeader.GetMembers(ent.Owner))
        {
            RaiseLocalEvent(member, ref ev);
        }
    }

    private void OnSpawnNestAction(Entity<ReplicatorQueenComponent> ent, ref ReplicatorSpawnNestActionEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var xform = Transform(ent);
        if (xform.MapID == MapId.Nullspace || !xform.Coordinates.IsValid(EntityManager))
        {
            this.AssertOrLogError(
                $"{ToPrettyString(ent)} tried to spawn a nest in an invalid position: {xform.Coordinates}");
            return;
        }

        _replicatorNest.SpawnNest(xform.Coordinates, _hiveLeader.GetMembers(ent.Owner));

        if (_replicatorQuery.ResolveOrNull(ent) is { } replicator)
        {
            _replicator.UpgradeReplicator(replicator, ent.Comp.UpgradeToAfterNestCreation);
        }

        // The queen's purpose is fulfilled. Remove its status.
        RemCompDeferred<ReplicatorQueenComponent>(ent);
        RemCompDeferred<ReplicatorHiveLeaderComponent>(ent);
    }

    private void ReplicatorSignAdded(Entity<ReplicatorQueenSignComponent> ent, ref ComponentStartup args)
    {
        _appearance.SetData(ent, ReplicatorVisuals.Queen, ent.Comp.Sprite);
    }

    private void ReplicatorSignRemoved(Entity<ReplicatorQueenSignComponent> ent, ref ComponentShutdown args)
    {
        _appearance.RemoveData(ent, ReplicatorVisuals.Queen);
    }
}
