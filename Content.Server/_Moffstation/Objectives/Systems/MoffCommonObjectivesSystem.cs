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

/// <summary>
/// Keeps the objectives of entities with <see cref="MoffCommonObjectivesComponent"/> mirrored from
/// their <see cref="MoffCommonObjectivesComponent.Authority"/>. While the authority has no
/// objectives (or no authority is set) the follower is given a placeholder objective instead so its
/// character menu isn't empty.
/// </summary>
/// <remarks>
/// Mirroring is driven by <see cref="ObjectiveAddedEvent"/>/<see cref="ObjectiveRemovedEvent"/>, so
/// followers pick changes up the moment the authority's mind is touched. The update loop only exists
/// to find the authority in the first place, since nothing announces that.
/// </remarks>
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
            // Resolve the authority once from the game rules, then cache it and follow it from there.
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

    /// <summary>
    /// Finds the authority for a follower by walking the active antag rules: the rule the follower
    /// belongs to is scanned for the member whose live mob carries
    /// <see cref="MoffSharedObjectiveAuthorityComponent"/>.
    /// </summary>
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
                    HasComp<MoffSharedObjectiveAuthorityComponent>(mob))
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

        // Objectives on their way out (e.g. a spent objective pack) must not be mirrored, and a
        // queued deletion doesn't mark the entity as terminating yet.
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

            var index = ownerMind.Objectives.IndexOf(objective);
            if (index >= 0)
                _mind.TryRemoveObjective(ownerMindId, ownerMind, index);
        }

        foreach (var objective in target)
        {
            if (!ownerMind.Objectives.Contains(objective))
                _mind.AddObjective(ownerMindId, ownerMind, objective);
        }
    }
}
