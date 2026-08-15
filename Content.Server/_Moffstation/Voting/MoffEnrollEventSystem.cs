using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared._ES.Voting;
using Content.Shared._ES.Voting.Components;
using Content.Shared._Moffstation.Extensions;
using Content.Shared._Moffstation.Voting.Components;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost.Components;
using Content.Shared.Roles.Components;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Moffstation.Voting;

public sealed partial class MoffEnrollEventSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<AntagSelectionComponent> _antagSelectionQuery;
    [Dependency] private EntityQuery<AntagLoadProfileRuleComponent> _antagProfileRuleQuery;
    [Dependency] private EntityQuery<GameRuleComponent> _gameRuleQuery;
    [Dependency] private EntityQuery<MoffEnrollEventComponent> _enrollEventQuery;
    [Dependency] private EntityQuery<GhostComponent> _ghostQuery;

    private readonly List<Entity<MoffEnrollEventComponent>> _enrollments = new(); // Reuse list for iteration in `Update` without new allocations.

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<ESVoterComponent>(ESVoterUiKey.Key, subs =>
        {
            subs.Event<MoffSetEnrollMessage>(OnSetEnroll);
            subs.Event<MoffSetEnrollRandomMessage>(OnSetEnrollRandom);
            subs.Event<MoffEarlyStartEnrollRequest>(OnEarlyStartEnrollRequest);
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _enrollments.Clear();
        foreach (var enrollment in EntityQueryEnumerator<MoffEnrollEventComponent>())
        {
            _enrollments.Add(enrollment);
        }
        foreach (var enroll in _enrollments)
        {
            if (_timing.CurTime < enroll.Comp.EndTime)
                continue;

            var ev = new MoffEndEnrollEvent();
            RaiseLocalEvent(enroll, ref ev);
        }
        _enrollments.Clear();
    }

    /// <summary>
    /// Resolves all the things needed to make the rule run as we want it.
    /// Sets up the map, warp, color, maxcount, and other stuff that only needs to be done once
    /// We do this here because
    /// <see cref="MoffEnrollEventComponent.OwningRule"/> prevents it from re-running.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnVoteSpawned(Entity<MoffEnrollEventComponent> ent, ref ESVoteEntitySpawnedEvent ev)
    {
        ent.Comp.EndTime = _timing.CurTime + ent.Comp.Duration;

        if (!_antagSelectionQuery.TryComp(ev.Manager, out var antag))
            return;

        TryMarkRuleAdded(ev.Manager);

        ent.Comp.OwningRule = ev.Manager;

        if (GetWarpTarget((ev.Manager, antag)) is { } mapCoords)
            ent.Comp.WarpTarget = GetNetCoordinates(_transform.ToCoordinates(mapCoords));

        ent.Comp.CharacterSelection = _antagProfileRuleQuery.HasComp(ev.Manager);
        if (GetAntagColor(antag) is { } color)
            ent.Comp.TitleColor = color;

        ent.Comp.MaxEnrolled = GetAntagSlotCount((ev.Manager, antag));

        Dirty(ent);
    }

    private void OnSetEnroll(Entity<ESVoterComponent> ent, ref MoffSetEnrollMessage args)
    {
        // Ghosts only - assignment bypasses the mutual-antag check, so this gate is what stops an embodied
        // already-antag from getting force-assigned a second, exclusive one.
        if (!_ghostQuery.HasComp(args.Actor))
            return;

        if (!TryGetEntity(args.Enroller, out var enrollerUid) ||
            !_enrollEventQuery.TryComp(enrollerUid, out var comp))
            return;

        if (args.Enrolled)
        {
            comp.Enrolled.Add(args.Actor);
        }
        else
        {
            comp.Enrolled.Remove(args.Actor);
            // Their character choice goes with them, so re-enrolling starts from their own character again.
            comp.RandomPick.Remove(GetNetEntity(args.Actor));
        }

        Dirty(enrollerUid.Value, comp);
    }

    private void OnSetEnrollRandom(Entity<ESVoterComponent> ent, ref MoffSetEnrollRandomMessage args)
    {
        // Ghosts only, matching OnSetEnroll - a random-character pick is only meaningful for an enrollee.
        if (!_ghostQuery.HasComp(args.Actor))
            return;

        if (!TryGetEntity(args.Enroller, out var enrollerUid) ||
            !_enrollEventQuery.TryComp(enrollerUid, out var comp) ||
            !comp.CharacterSelection)
            return;

        var netAttached = GetNetEntity(args.Actor);
        if (args.Random)
            comp.RandomPick.Add(netAttached);
        else
            comp.RandomPick.Remove(netAttached);

        Dirty(enrollerUid.Value, comp);
    }

    private void OnEarlyStartEnrollRequest(Entity<ESVoterComponent> ent, ref MoffEarlyStartEnrollRequest args)
    {
        if (!_player.TryGetSessionByEntity(args.Actor, out var session) ||
            !_admin.HasAdminFlag(session, AdminFlags.Fun))
            return;

        if (!TryGetEntity(args.EnrollEvent, out var enrollUid) ||
            !_enrollEventQuery.TryComp(enrollUid, out var comp))
            return;

        comp.EndTime = _timing.CurTime;
        Dirty(enrollUid.Value, comp);

        _adminLogger.Add(LogType.Action,
            LogImpact.Extreme,
            $"{session:player} started enrollment event {ToPrettyString(enrollUid.Value):enrollEvent} early");
    }

    [SubscribeLocalEvent]
    private void OnEnrollEventEnded(Entity<MoffEnrollEventComponent> ent, ref MoffEndEnrollEvent args)
    {
        var rule = ent.Comp.OwningRule;

        // Resolve the enrolled player entities into sessions.
        var sessions = new List<ICommonSession>();
        foreach (var player in ent.Comp.Enrolled)
        {
            if (!_player.TryGetSessionByEntity(player, out var session))
                continue;

            sessions.Add(session);
        }

        // Shuffled first so that capping to the antag's slot count takes a random subset.
        _random.Shuffle(sessions);
        var enrolled = sessions.Take(ent.Comp.MaxEnrolled);
        var enrolledCount = Math.Min(sessions.Count, ent.Comp.MaxEnrolled);

        // Too few enrollees, or no rule to hand antags out - run the fallback instead.
        if (enrolledCount < ent.Comp.MinEnrolled ||
            rule is not { } ruleUid ||
            !_antagSelectionQuery.TryComp(ruleUid, out var antag))
        {
            FireFallbackRule(ent);
            TryQueueDel(rule);
            return;
        }

        // Start the rule
        TryMarkRuleAdded(ruleUid);
        _gameTicker.StartGameRule(ruleUid);

        var players = _antag.GetActivePlayerCount();
        var gameRule = new Entity<AntagSelectionComponent>(ruleUid, antag);
        foreach (var session in enrolled)
        {
            // ignoreExclusivity: true - an enrolling ghost may already be an antag. Bans/validity still apply.
            _antag.TryAssignNextAvailableAntag(gameRule, session, players, checkPref: false, ignoreExclusivity: true);
        }
        TryQueueDel(rule);
    }

    /// <summary>
    /// Where this event's antag spawns, asked of the rule the same way antag selection does. Resolved once
    /// when the rule's added (see <see cref="OnVoteSpawned"/>) and stored on the component as the warp target.
    /// Returns null when there isn't a valid location to warp to.
    /// </summary>
    private MapCoordinates? GetWarpTarget(Entity<AntagSelectionComponent> rule)
    {
        foreach (var selector in rule.Comp.Antags)
        {
            if (!_proto.Resolve(selector.Proto, out var def))
                continue;

            var ev = new AntagSelectLocationEvent(rule, def);
            RaiseLocalEvent(rule, ref ev, true);

            if (ev.Coordinates.Count > 0)
                return _random.Pick(ev.Coordinates);
        }

        return null;
    }

    /// <summary>
    /// Marks a vote-manager rule as added, raising <see cref="GameRuleAddedEvent"/>
    /// its components set up their add-time state notably SpaceSpawnRule,
    /// which picks the antag spawn on add. Idempotent; returns whether it flipped the rule to added.
    /// </summary>
    private void TryMarkRuleAdded(EntityUid ruleUid)
    {
        if (!_gameRuleQuery.TryComp(ruleUid, out var gameRule) || gameRule.Added)
            return;

        gameRule.Added = true;
        var addedEv = new GameRuleAddedEvent(ruleUid, MetaData(ruleUid).EntityPrototype?.ID ?? string.Empty);
        RaiseLocalEvent(ruleUid, ref addedEv, true);
    }

    /// <summary>
    /// The color of the antag this rule hands out: its mind role's subtype color, falling back to the
    /// color of that mind role's role type.
    /// </summary>
    private Color? GetAntagColor(AntagSelectionComponent antag)
    {
        foreach (var selector in antag.Antags)
        {
            if (!_proto.Resolve(selector.Proto, out var def) || def.MindRoles is not { } mindRoles)
                continue;

            foreach (var mindRoleId in mindRoles)
            {
                if (!_proto.Resolve(mindRoleId, out var mindRoleProto) ||
                    !mindRoleProto.TryComp<MindRoleComponent>(out var mindRole, EntityManager.ComponentFactory))
                    continue;

                if (mindRole.SubtypeColor is { } subtypeColor)
                    return subtypeColor;

                if (mindRole.RoleType is { } roleType && _proto.Resolve(roleType, out var roleTypeProto))
                    return roleTypeProto.Color;
            }
        }

        return null;
    }

    private int GetAntagSlotCount(Entity<AntagSelectionComponent> ent)
    {
        var players = _antag.GetActivePlayerCount();

        return _antag.GetTotalAntagCount(ent, players);
    }

    private void FireFallbackRule(Entity<MoffEnrollEventComponent> ent)
    {
        // Not enough people enrolled, fire the fallback rule
        foreach (var proto in _entityTable.GetSpawns(ent.Comp.FallbackRules, _random))
        {
            _gameTicker.StartGameRule(proto);
        }
    }

    public bool EnrolleeWantsRandom(EntityUid rule, ICommonSession session)
    {
        if (session.AttachedEntity is not { } attached
            || !TryComp<ESSynchronizedVoteManagerComponent>(rule, out var voteManager))
            return false;

        return _enrollEventQuery.ResolveAll(voteManager.VoteEntities, logMissing: false)
            .FirstOrNull()
            ?.Comp.RandomPick.Contains(GetNetEntity(attached)) ?? false;
    }
}

[ByRefEvent]
public record struct MoffEndEnrollEvent();
