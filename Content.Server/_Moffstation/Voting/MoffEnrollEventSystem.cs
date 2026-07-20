using System.Linq;
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
using Robust.Shared.Network;
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Collect first: resolving an enrollment starts game rules and deletes entities, which would
        // invalidate the query enumerator if done inline.
        var expired = new List<Entity<MoffEnrollEventComponent>>();
        var query = EntityQueryEnumerator<MoffEnrollEventComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Resolve the owning rule once; it's all fixed prototype data after, so a non-null OwningRule skips it.
            if (comp.OwningRule is null)
                ResolveOwningRule((uid, comp));

            if (_timing.CurTime > comp.EndTime)
                expired.Add((uid, comp));
        }

        foreach (var ent in expired)
        {
            ResolveEnrollment(ent);
            QueueDel(ent.Owner);
        }
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
            GetWarpTarget((enrollUid.Value, enroll)) is not { } coords)
            return;

        _transform.SetMapCoordinates(player, coords);
    }

    /// <summary>
    /// Where this event's antag spawns, asked of the rule the same way antag selection does. Only works
    /// once the rule's resolved, see <see cref="ResolveOwningRule"/>.
    /// </summary>
    private MapCoordinates? GetWarpTarget(Entity<MoffEnrollEventComponent> ent)
    {
        if (ent.Comp.OwningRule is not { } ruleUid ||
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
    /// Resolves the owning rule once. Adds it mid-vote - loads its map and picks the antag spawn, which is
    /// what "Go To" warps to, and pulls the antag-derived fields (title color, whether a character can be
    /// picked). Adding's invisible since these rules have no start announcement/audio; the rule only really
    /// starts (spawning the antag) when the vote resolves. All fixed once the rule exists, so a non-null
    /// <see cref="MoffEnrollEventComponent.OwningRule"/> stops it re-running.
    /// </summary>
    private void ResolveOwningRule(Entity<MoffEnrollEventComponent> ent)
    {
        if (FindOwningRule(ent.Owner) is not { } ruleUid ||
            !TryComp<AntagSelectionComponent>(ruleUid, out var antag))
            return;

        TryMarkRuleAdded(ruleUid);

        ent.Comp.OwningRule = ruleUid;
        // The spawn location exists now, so ghosts can go and look at it.
        ent.Comp.Warpable = true;
        ent.Comp.CharacterSelection = GetCharacterSelection(antag);
        if (GetAntagColor(antag) is { } color)
            ent.Comp.TitleColor = color;

        ent.Comp.MaxEnrolled = GetAntagSlotCount(antag);

        Dirty(ent);
    }

    /// <summary>
    /// Marks a vote-manager rule (whose start <c>GameTicker.AddGameRule</c> deferred) as added, raising
    /// <see cref="GameRuleAddedEvent"/> so its components set up their add-time state - notably SpaceSpawnRule,
    /// which picks the antag spawn on add. Idempotent; returns whether it flipped the rule to added.
    /// </summary>
    private bool TryMarkRuleAdded(EntityUid ruleUid)
    {
        if (!TryComp<GameRuleComponent>(ruleUid, out var gameRule) || gameRule.Added)
            return false;

        gameRule.Added = true;
        var addedEv = new GameRuleAddedEvent(ruleUid, MetaData(ruleUid).EntityPrototype?.ID ?? string.Empty);
        RaiseLocalEvent(ruleUid, ref addedEv, true);
        return true;
    }

    /// <summary>
    /// Whether to show the character picker. Hidden when the antag spawns a fixed non-humanoid body:
    /// <c>AllowNonHumans</c> doubles as "not built from the player's profile" (those rules have no
    /// <c>AntagLoadProfileRuleComponent</c>, so a picked character does nothing). Any non-humanoid specifier
    /// hides it, since a multi-specifier rule could go either way.
    /// </summary>
    private bool GetCharacterSelection(AntagSelectionComponent antag)
    {
        foreach (var selector in antag.Antags)
        {
            if (_proto.TryIndex(selector.Proto, out var def) && def.AllowNonHumans)
                return false;
        }

        return true;
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

    private int GetAntagSlotCount(AntagSelectionComponent antag)
    {
        var players = _antag.GetActivePlayerCount();

        return antag.Antags.Sum(selector => selector.GetTargetAntagCount(_random, players));
    }

    /// <summary>
    /// Users who asked to spawn as a random character. Only filled while <see cref="ResolveEnrollment"/>
    /// assigns antags, which is when the body gets built.
    /// </summary>
    private readonly HashSet<NetUserId> _randomProfiles = new();

    /// <summary>
    /// Whether this player enrolled asking for a random character over their selected one. Only meaningful
    /// while an enrollment resolves - the only time an enrollee's body gets built.
    /// </summary>
    public bool PrefersRandomProfile(ICommonSession session) => _randomProfiles.Contains(session.UserId);

    private void ResolveEnrollment(Entity<MoffEnrollEventComponent> ent)
    {
        var rule = ent.Comp.OwningRule;

        try
        {
            // Resolve the enrolled player entities into sessions.
            var sessions = new List<ICommonSession>();
            foreach (var netEntity in ent.Comp.Enrolled)
            {
                if (!TryGetEntity(netEntity, out var player) ||
                    !_player.TryGetSessionByEntity(player.Value, out var session))
                    continue;

                sessions.Add(session);
                if (ent.Comp.RandomPick.Contains(netEntity))
                    _randomProfiles.Add(session.UserId);
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
                    // ignoreExclusivity: true - an enrolling ghost may already be an antag. Bans/validity still apply.
                    _antag.TryAssignNextAvailableAntag((ruleUid, antag), session, players, checkPref: false, ignoreExclusivity: true);
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
        finally
        {
            _randomProfiles.Clear();
        }
    }

    /// <summary>
    /// Starts the owning rule once enrollment's done. Normally already added by <see cref="ResolveOwningRule"/>
    /// during the vote; the <see cref="TryMarkRuleAdded"/> call just covers the edge case where it wasn't.
    /// </summary>
    private void StartOwningRule(EntityUid ruleUid)
    {
        TryMarkRuleAdded(ruleUid);
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
