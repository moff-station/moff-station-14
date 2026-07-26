using Content.Shared._Moffstation.Extensions;
using Content.Shared._Moffstation.Replicator.Components;
using JetBrains.Annotations;

namespace Content.Shared._Moffstation.Replicator.Systems;

/// This system implements an API for interacting with <see cref="ReplicatorHiveLeaderComponent.Members"/>.
public sealed partial class ReplicatorHiveLeaderSystem : EntitySystem
{
    [Dependency] private EntityQuery<ReplicatorHiveLeaderComponent> _query;
    [Dependency] private EntityQuery<ReplicatorComponent> _replicatorQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplicatorHiveLeaderComponent, MemberReplicatorUpgradedEvent>(OnReplicatorUpgraded);
    }

    /// Returns all of the <see cref="ReplicatorHiveLeaderComponent.Members"/> of <paramref name="entity"/>.
    [PublicAPI]
    public IEnumerable<Entity<ReplicatorComponent>> GetMembers(Entity<ReplicatorHiveLeaderComponent?> entity) =>
        _query.ResolveOrNull(entity) is { } leader ? _replicatorQuery.ResolveAll(leader.Comp.Members) : [];

    /// Adds <paramref name="members"/> to <see cref="ReplicatorHiveLeaderComponent.Members"/> of <paramref name="entity"/>.
    [PublicAPI]
    public void AssignReplicators(
        Entity<ReplicatorHiveLeaderComponent?> entity,
        IEnumerable<Entity<ReplicatorComponent>> members
    )
    {
        if (_query.ResolveOrNull(entity) is not { } hiveLeader)
            return;

        hiveLeader.Comp.Members.AddRange(members.Owners());
        Dirty(hiveLeader);

        var ev = new HiveLeaderAssignedEvent(hiveLeader);
        foreach (var uid in hiveLeader.Comp.Members)
        {
            RaiseLocalEvent(uid, ref ev);
        }
    }


    /// When a replicator is upgraded, we need to update its leader's member list to remove the old entity and include
    /// the new one.
    private void OnReplicatorUpgraded(
        Entity<ReplicatorHiveLeaderComponent> entity,
        ref MemberReplicatorUpgradedEvent args
    )
    {
        args.UpgradedTo.Comp.HiveLeader = entity;
        entity.Comp.Members.Remove(args.UpgradedFrom);
        entity.Comp.Members.Add(args.UpgradedTo);
        Dirty(entity);
    }
}
