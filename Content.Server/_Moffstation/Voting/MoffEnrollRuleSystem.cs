using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Shared._ES.Voting.Components;
using Content.Shared._Moffstation.Voting.Components;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Voting;

/// <summary>
/// Server-side resolution of <see cref="MoffEnrollEventComponent"/> enrollment votes. When the enroll
/// timer runs out, the players who enrolled are assigned as the owning game rule's antags. If fewer
/// than <see cref="MoffEnrollEventComponent.MinEnrolled"/> enrolled, a configurable fallback game rule
/// is started instead.
/// </summary>
/// <remarks>
/// The enroll entity is spawned by an <see cref="ESSynchronizedVoteManagerComponent"/> which lives on
/// the game rule itself (together with the <see cref="AntagSelectionComponent"/>). That rule is
/// deliberately not started by <c>GameTicker.AddGameRule</c> while it has a vote manager, so we start
/// it here once enrollment concludes.
/// </remarks>
public sealed partial class MoffEnrollRuleSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Collect first: resolving an enrollment starts game rules and deletes entities, which would
        // invalidate the query enumerator if done inline.
        var expired = new List<Entity<MoffEnrollEventComponent>>();
        var query = EntityQueryEnumerator<MoffEnrollEventComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime > comp.EndTime)
                expired.Add((uid, comp));
        }

        foreach (var ent in expired)
        {
            ResolveEnrollment(ent);
            QueueDel(ent.Owner);
        }
    }

    private void ResolveEnrollment(Entity<MoffEnrollEventComponent> ent)
    {
        var rule = FindOwningRule(ent.Owner);

        // Resolve the enrolled player entities into sessions.
        var sessions = new List<ICommonSession>();
        foreach (var netEntity in ent.Comp.Enrolled)
        {
            if (TryGetEntity(netEntity, out var player) &&
                _player.TryGetSessionByEntity(player.Value, out var session))
            {
                sessions.Add(session);
            }
        }

        // Cap the number of assigned players if MaxEnrolled is set (0 == unlimited).
        if (ent.Comp.MaxEnrolled > 0 && sessions.Count > ent.Comp.MaxEnrolled)
        {
            _random.Shuffle(sessions);
            sessions.RemoveRange(ent.Comp.MaxEnrolled, sessions.Count - ent.Comp.MaxEnrolled);
        }

        // Enough players enrolled and we found the antag rule: start it and assign the enrolled players.
        if (sessions.Count >= ent.Comp.MinEnrolled &&
            rule is { } ruleUid &&
            TryComp<AntagSelectionComponent>(ruleUid, out var antag))
        {
            StartOwningRule(ruleUid);

            var players = _antag.GetActivePlayerCount();
            foreach (var session in sessions)
            {
                // checkPref: false - enrolling is explicit consent, so we bypass antag preferences
                // (bans / validity are still enforced).
                _antag.TryAssignNextAvailableAntag((ruleUid, antag), session, players, checkPref: false);
            }

            return;
        }

        // Not enough enrolled (or no owning antag rule): fire the fallback rules instead and clean up
        // the antag rule that was spawned but never started.
        foreach (var proto in _entityTable.GetSpawns(ent.Comp.FallbackRules, _random))
        {
            _gameTicker.StartGameRule(proto);
        }

        if (rule is { } unstartedRule)
            QueueDel(unstartedRule);
    }

    /// <summary>
    /// Starts a rule whose start was deferred by <c>GameTicker.AddGameRule</c> because it carries an
    /// <see cref="ESSynchronizedVoteManagerComponent"/>. Raises <see cref="GameRuleAddedEvent"/> first
    /// (mirroring ESVoteSystem) so rule components initialize their added-time state - notably
    /// SpaceSpawnRule, which computes the antag spawn location on add - then starts the rule.
    /// </summary>
    private void StartOwningRule(EntityUid ruleUid)
    {
        if (TryComp<GameRuleComponent>(ruleUid, out var gameRule) && !gameRule.Added)
        {
            gameRule.Added = true;
            var addedEv = new GameRuleAddedEvent(ruleUid, MetaData(ruleUid).EntityPrototype?.ID ?? string.Empty);
            RaiseLocalEvent(ruleUid, ref addedEv, true);
        }

        _gameTicker.StartGameRule(ruleUid);
    }

    /// <summary>
    /// Finds the synchronized vote manager (the game rule) that spawned this enroll entity.
    /// </summary>
    private EntityUid? FindOwningRule(EntityUid enrollUid)
    {
        var query = EntityQueryEnumerator<ESSynchronizedVoteManagerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var voteEntities = comp.VoteEntities;
            if (voteEntities.Contains(enrollUid))
                return uid;
        }

        return null;
    }
}
