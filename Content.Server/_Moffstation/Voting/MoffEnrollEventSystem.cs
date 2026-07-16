using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Shared._ES.Voting.Components;
using Content.Shared._Moffstation.Voting.Components;
using Content.Shared._Moffstation.Voting.Systems;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost;
using Content.Shared.Roles.Components;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
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
public sealed partial class MoffEnrollEventSystem : SharedMoffEnrollEventSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<MoffEnrollGotoMessage>(OnGoto);
    }

    /// <summary>
    /// Warps a ghost to where this event will spawn, so they can have a look before enrolling.
    /// </summary>
    private void OnGoto(MoffEnrollGotoMessage args, EntitySessionEventArgs ev)
    {
        // Only ghosts get to fly off and look around.
        if (ev.SenderSession.AttachedEntity is not { } player || !HasComp<GhostComponent>(player))
            return;

        if (!TryGetEntity(args.Enroller, out var enrollUid) ||
            !TryComp<MoffEnrollEventComponent>(enrollUid, out var enroll) ||
            !enroll.Warpable ||
            GetWarpTarget(enrollUid.Value) is not { } coords)
            return;

        _transform.SetMapCoordinates(player, coords);
    }

    /// <summary>
    /// Where this event's antag will spawn, asked of the rule the same way antag selection asks for it.
    /// Only resolves once the rule has been added, see <see cref="EnsureOwningRuleAdded"/>.
    /// </summary>
    private MapCoordinates? GetWarpTarget(EntityUid enrollUid)
    {
        if (FindOwningRule(enrollUid) is not { } ruleUid ||
            !TryComp<AntagSelectionComponent>(ruleUid, out var antag))
            return null;

        foreach (var selector in antag.Antags)
        {
            if (!_proto.TryIndex(selector.Proto, out var def))
                continue;

            var ev = new AntagSelectLocationEvent((ruleUid, antag), def);
            RaiseLocalEvent(ruleUid, ref ev, true);

            if (ev.Coordinates.Count > 0)
                return _random.Pick(ev.Coordinates);
        }

        return null;
    }

    /// <summary>
    /// Adds the owning rule while the vote is still running, so its map is loaded and its antag spawn
    /// location is picked up front - that is what "Go To" warps to. This is player-invisible for these
    /// rules since none of them set a station event start announcement or audio. Actually starting the
    /// rule (and so spawning the antag) still waits until the vote resolves.
    /// </summary>
    private void EnsureOwningRuleAdded(Entity<MoffEnrollEventComponent> ent)
    {
        if (FindOwningRule(ent.Owner) is not { } ruleUid ||
            !TryComp<GameRuleComponent>(ruleUid, out var gameRule) ||
            gameRule.Added)
            return;

        gameRule.Added = true;
        var addedEv = new GameRuleAddedEvent(ruleUid, MetaData(ruleUid).EntityPrototype?.ID ?? string.Empty);
        RaiseLocalEvent(ruleUid, ref addedEv, true);

        // The spawn location exists now, so ghosts can go and look at it.
        ent.Comp.Warpable = true;
        Dirty(ent);
    }

    private void UpdateTitleColor(Entity<MoffEnrollEventComponent> ent)
    {
        if (FindOwningRule(ent.Owner) is not { } ruleUid ||
            !TryComp<AntagSelectionComponent>(ruleUid, out var antag) ||
            GetAntagColor(antag) is not { } color ||
            ent.Comp.TitleColor == color)
            return;

        ent.Comp.TitleColor = color;
        Dirty(ent);
    }

    /// <summary>
    /// The colour of the antag this rule hands out: its mind role's subtype colour, falling back to the
    /// colour of that mind role's role type.
    /// </summary>
    private Color? GetAntagColor(AntagSelectionComponent antag)
    {
        foreach (var selector in antag.Antags)
        {
            if (!_proto.TryIndex(selector.Proto, out var def) || def.MindRoles is not { } mindRoles)
                continue;

            foreach (var mindRoleId in mindRoles)
            {
                if (!_proto.TryIndex(mindRoleId, out var mindRoleProto) ||
                    !mindRoleProto.TryComp<MindRoleComponent>(out var mindRole, EntityManager.ComponentFactory))
                    continue;

                if (mindRole.SubtypeColor is { } subtypeColor)
                    return subtypeColor;

                if (mindRole.RoleType is { } roleType && _proto.TryIndex(roleType, out var roleTypeProto))
                    return roleTypeProto.Color;
            }
        }

        return null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Collect first: resolving an enrollment starts game rules and deletes entities, which would
        // invalidate the query enumerator if done inline.
        var expired = new List<Entity<MoffEnrollEventComponent>>();
        var query = EntityQueryEnumerator<MoffEnrollEventComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            EnsureOwningRuleAdded((uid, comp));
            UpdateTitleColor((uid, comp));

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
