using System.Linq;
using Content.Server._Moffstation.Objectives.Components;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Shared._Moffstation.Extensions;
using Content.Shared._Moffstation.Objectives;
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

    [Dependency] private EntityQuery<MoffCommonObjectivesComponent> _followerQuery;
    [Dependency] private EntityQuery<MoffCommonObjectiveAuthorityComponent> _authorityQuery;

    [SubscribeLocalEvent]
    private void OnAntagSelected(ref AfterAntagEntitySelectedEvent ev)
    {
        if (_authorityQuery.HasComp(ev.EntityUid))
            BindRuleFollowers(ev.GameRule, ev.EntityUid);
        else if (_followerQuery.TryComp(ev.EntityUid, out var follower))
        {
            BindToAuthorityByRule((ev.EntityUid, follower), ev.GameRule);
            SetupPlaceholderObjective((ev.EntityUid, follower));
        }
    }

    /// <summary>
    /// Binds every follower already selected in <paramref name="rule"/> to a newly selected authority.
    /// </summary>
    private void BindRuleFollowers(Entity<AntagSelectionComponent> rule, EntityUid authority)
    {
        if (!_mind.TryGetMind(authority, out var authorityMind, out _))
            return;

        foreach (var member in _antag.GetAntagMinds(rule.Owner))
        {
            if (member.Comp.CurrentEntity is { } mob &&
                mob != authority &&
                _followerQuery.TryComp(mob, out var follower))
            {
                SetAuthority((mob, follower), authorityMind);
            }
        }
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
    /// Binds a newly selected follower to <paramref name="rule"/>'s authority, if one exists.
    /// </summary>
    private void BindToAuthorityByRule(Entity<MoffCommonObjectivesComponent> ent, Entity<AntagSelectionComponent> rule)
    {
        foreach (var member in _antag.GetAntagMinds(rule.Owner))
        {
            if (member.Comp.CurrentEntity is { } mob && mob != ent.Owner && _authorityQuery.HasComp(mob))
            {
                SetAuthority(ent, member.Owner);
                return;
            }
        }
    }

    /// <summary>
    /// Makes <paramref name="ent"/> copy its objectives from <paramref name="authorityMind"/> from now on.
    /// </summary>
    public void SetAuthority(Entity<MoffCommonObjectivesComponent> ent, EntityUid authorityMind)
    {
        if (ent.Comp.AuthorityMind == authorityMind)
            return;

        if (ent.Comp.AuthorityMind is { } old && _authorityQuery.TryComp(old, out var oldAuthority))
            oldAuthority.Followers.Remove(ent);

        ent.Comp.AuthorityMind = authorityMind;
        EnsureComp<MoffCommonObjectiveAuthorityComponent>(authorityMind).Followers.Add(ent);
        SyncObjectives(ent);
    }

    /// <summary>
    /// Re-syncs every follower that is currently following <paramref name="authorityMind"/>.
    /// </summary>
    private void SyncFollowersOf(EntityUid authorityMind)
    {
        if (!_authorityQuery.TryComp(authorityMind, out var authority))
            return;

        foreach (var follower in authority.Followers.ToArray())
        {
            // Drop followers that were deleted or bound to someone else.
            if (!_followerQuery.TryComp(follower, out var comp) || comp.AuthorityMind != authorityMind)
            {
                authority.Followers.Remove(follower);
                continue;
            }

            SyncObjectives((follower, comp));
        }
    }

    private void SyncObjectives(Entity<MoffCommonObjectivesComponent> ent)
    {
        if (!_mind.TryGetMind(ent, out var ownerMindId, out var ownerMind))
            return;

        if (ent.Comp.AuthorityMind is not { } authorityMind ||
            !TryComp<MindComponent>(authorityMind, out var authMind))
        {
            return;
        }

        var authorityObjectives = authMind.Objectives.ToList();
        if (authorityObjectives.Count == 0)
            return;

        // Backwards, since removing shifts every index after it.
        for (var i = ownerMind.Objectives.Count - 1; i >= 0; i--)
        {
            if (!authorityObjectives.Contains(ownerMind.Objectives[i]))
                _mind.TryRemoveObjective(ownerMindId, ownerMind, i);
        }

        foreach (var objective in authorityObjectives)
        {
            if (!ownerMind.Objectives.Contains(objective))
                _mind.AddObjective(ownerMindId, ownerMind, objective);
        }
    }

    private void SetupPlaceholderObjective(Entity<MoffCommonObjectivesComponent> ent)
    {
        if (ent.Comp.PlaceHolderObject != null)
            return;

        if (!_mind.TryGetMind(ent.Owner, out var mindId, out var mindComp))
            return;

        if (_objectives.TryCreateObjective(mindId, mindComp, ent.Comp.PlaceholderProtoId) is not { } objective)
            return;

        ent.Comp.PlaceHolderObject = objective;
        _mind.AddObjective(mindId, mindComp, objective);
    }
}
