using System.Linq;
using Content.Server._Moffstation.Objectives.Components;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Shared._Moffstation.Objectives;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Objectives.Systems;

public sealed partial class MoffCommonObjectivesSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedObjectivesSystem _objectives = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextSync;

    public override void Update(float frameTime)
    {
        if (_timing.CurTime < _nextSync)
            return;
        _nextSync = _timing.CurTime + SyncInterval;

        var query = EntityQueryEnumerator<MoffCommonObjectivesComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Authority == null &&
                TryResolveAuthority((uid, comp), out var found))
            {
                comp.Authority = found;
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(Entity<MoffCommonObjectivesComponent> ent, ref PlayerAttachedEvent ev)
    {
        if (ent.Comp.PlaceHolder != null)
            return;

        if (!_mind.TryGetMind(ev.Player, out var mindId, out var mindComp))
            return;

        if (_objectives.TryCreateObjective(mindId, mindComp, ent.Comp.PlaceholderProtoId) is not { } objective)
            return;

        ent.Comp.PlaceHolder = objective;
        _mind.AddObjective(mindId, mindComp, objective);
    }

    [SubscribeLocalEvent]
    private void OnObjectiveAdded(ref ObjectiveAddedEvent ev)
    {
        SyncFollowersOf(ev.Mind);
    }

    [SubscribeLocalEvent]
    private void OnObjectiveRemoved(ref ObjectiveRemovedEvent ev)
    {
        SyncFollowersOf(ev.Mind);
    }

    /// <summary>
    /// Re-syncs every follower that is currently following <paramref name="authorityMind"/>.
    /// </summary>
    private void SyncFollowersOf(EntityUid authorityMind)
    {
        var query = EntityQueryEnumerator<MoffCommonObjectivesComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Authority is not { } authority ||
                !_mind.TryGetMind(authority, out var mindId, out _) ||
                mindId != authorityMind)
                continue;

            SyncObjectives((uid, comp));
        }
    }

    private bool TryResolveAuthority(Entity<MoffCommonObjectivesComponent> ent, out EntityUid authority)
    {
        authority = default;

        if (!_mind.TryGetMind(ent.Owner, out var mindId, out _))
            return false;

        var rules = EntityQueryEnumerator<AntagSelectionComponent, ActiveGameRuleComponent>();
        while (rules.MoveNext(out var ruleUid, out _, out _))
        {
            var members = _antag.GetAntagMinds(ruleUid).ToList();

            if (members.All(member => member.Owner != mindId))
                continue;

            foreach (var member in members)
            {
                if (member.Comp.CurrentEntity is { } mob &&
                    mob != ent.Owner &&
                    HasComp<MoffCommonObjectiveAuthorityComponent>(mob))
                {
                    authority = mob;
                    SyncObjectives(ent);
                    return true;
                }
            }
        }

        return false;
    }

    private void SyncObjectives(Entity<MoffCommonObjectivesComponent> ent)
    {
        var comp = ent.Comp;

        if (!_mind.TryGetMind(ent, out var ownerMindId, out var ownerMind))
            return;

        var target = comp.Authority is { } authority &&
                     _mind.TryGetMind(authority, out _, out var authMind)
            ? authMind.Objectives.ToList()
            : [];

        if (target.Count == 0)
            return;

        foreach (var objective in ownerMind.Objectives.ToArray())
        {
            if (target.Contains(objective))
                continue;

            _mind.TryRemoveObjective(ownerMindId, ownerMind, ownerMind.Objectives.IndexOf(objective));
        }

        foreach (var objective in target)
        {
            if (!ownerMind.Objectives.Contains(objective))
                _mind.AddObjective(ownerMindId, ownerMind, objective);
        }
    }
}
