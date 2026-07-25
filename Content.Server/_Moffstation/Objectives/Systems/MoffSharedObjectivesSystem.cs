using System.Linq;
using Content.Server._Moffstation.Objectives.Components;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Objectives.Systems;

/// <summary>
/// Keeps the objectives of entities with <see cref="MoffSharedObjectivesComponent"/> mirrored from
/// their <see cref="MoffSharedObjectivesComponent.Authority"/>. While the authority has no
/// objectives (or no authority is set) the follower is given a placeholder objective instead so its
/// character menu isn't empty.
/// </summary>
public sealed partial class MoffSharedObjectivesSystem : EntitySystem
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

        var query = EntityQueryEnumerator<MoffSharedObjectivesComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Resolve the authority once from the game rules, then cache it and follow it from there.
            if (comp.Authority == null &&
                TryResolveAuthority((uid, comp), out var found))
            {
                comp.Authority = found;
            }

            Sync((uid, comp));
        }
    }

    /// <summary>
    /// Finds the authority for a follower by walking the active antag rules: the rule the follower
    /// belongs to is scanned for the member whose live mob carries
    /// <see cref="MoffSharedObjectiveAuthorityComponent"/>.
    /// </summary>
    private bool TryResolveAuthority(Entity<MoffSharedObjectivesComponent> ent, out EntityUid authority)
    {
        authority = default;

        if (!_mind.TryGetMind(ent.Owner, out var mindId, out _))
            return false;

        // Every antag rule carries AntagSelectionComponent; ActiveGameRuleComponent means it's running.
        var rules = EntityQueryEnumerator<AntagSelectionComponent, ActiveGameRuleComponent>();
        while (rules.MoveNext(out var ruleUid, out _, out _))
        {
            var members = _antag.GetAntagMinds(ruleUid).ToList();

            // Only look inside the rule this follower is actually part of.
            if (members.All(member => member.Owner != mindId))
                continue;

            // The marked member is the authority. The marker sits on the live mob.
            foreach (var member in members)
            {
                if (member.Comp.CurrentEntity is { } mob &&
                    mob != ent.Owner &&
                    HasComp<MoffSharedObjectiveAuthorityComponent>(mob))
                {
                    authority = mob;
                    return true;
                }
            }
        }

        return false;
    }

    private void Sync(Entity<MoffSharedObjectivesComponent> ent)
    {
        var comp = ent.Comp;

        if (!_mind.TryGetMind(ent, out var ownerMindId, out var ownerMind))
            return;

        IReadOnlyList<EntityUid> target = comp.Authority is { } authority &&
                                          _mind.TryGetMind(authority, out _, out var authMind)
            ? authMind.Objectives
            : Array.Empty<EntityUid>();

        foreach (var objective in ownerMind.Objectives.ToArray())
        {
            if (objective == comp.PlaceHolder || target.Contains(objective))
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

        UpdatePlaceholder(ent, ownerMindId, ownerMind, wantPlaceholder: target.Count == 0);
    }

    private void UpdatePlaceholder(
        Entity<MoffSharedObjectivesComponent> ent,
        EntityUid ownerMindId,
        MindComponent ownerMind,
        bool wantPlaceholder)
    {
        var comp = ent.Comp;

        if (wantPlaceholder)
        {
            var alreadyPresent = comp.PlaceHolder is { } existing &&
                                 !Deleted(existing) &&
                                 ownerMind.Objectives.Contains(existing);
            if (alreadyPresent)
                return;

            var objective = _objectives.TryCreateObjective(ownerMindId, ownerMind, comp.PlaceholderProtoId);
            if (objective == null)
                return;

            _mind.AddObjective(ownerMindId, ownerMind, objective.Value);
            comp.PlaceHolder = objective.Value;
        }
        else if (comp.PlaceHolder is { } placeholder)
        {
            var index = ownerMind.Objectives.IndexOf(placeholder);
            if (index >= 0)
                _mind.TryRemoveObjective(ownerMindId, ownerMind, index);

            comp.PlaceHolder = null;
        }
    }
}
