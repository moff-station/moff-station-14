using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared._ES.Voting;
using Content.Shared._ES.Voting.Components;
using Content.Shared._Moffstation.Voting.Components;
using Content.Shared._Moffstation.Voting.Systems;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Content.Shared.Roles.Components;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Voting;

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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // invalidate the query enumerator if done inline.
        var query = EntityQueryEnumerator<MoffEnrollEventComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.EndTime)
                return;
            ResolveEnrollment((uid, comp));
            QueueDel(uid);
        }
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

        if (!TryComp<AntagSelectionComponent>(ev.Manager, out var antag))
            return;

        TryMarkRuleAdded(ev.Manager);

        ent.Comp.OwningRule = ev.Manager;

        if (GetWarpTarget((ev.Manager, antag)) is { } mapCoords)
            ent.Comp.WarpTarget = GetNetCoordinates(_transform.ToCoordinates(mapCoords));

        ent.Comp.CharacterSelection = HasComp<AntagLoadProfileRuleComponent>(ev.Manager);
        if (GetAntagColor(antag) is { } color)
            ent.Comp.TitleColor = color;

        ent.Comp.MaxEnrolled = GetAntagSlotCount((ev.Manager, antag));

        Dirty(ent);
    }

    private void ResolveEnrollment(Entity<MoffEnrollEventComponent> ent)
    {
        var rule = ent.Comp.OwningRule;

        // Resolve the enrolled player entities into sessions.
        var sessions = new List<ICommonSession>();
        foreach (var netEntity in ent.Comp.Enrolled)
        {
            if (!TryGetEntity(netEntity, out var player) ||
                !_player.TryGetSessionByEntity(player.Value, out var session))
                continue;

            sessions.Add(session);
        }

        // Cap the number of assigned players if MaxEnrolled is set.
        if (sessions.Count > ent.Comp.MaxEnrolled)
        {
            _random.Shuffle(sessions);
            sessions.RemoveRange(ent.Comp.MaxEnrolled, sessions.Count - ent.Comp.MaxEnrolled);
        }

        // Start the rule
        if (sessions.Count >= ent.Comp.MinEnrolled &&
            rule is { } ruleUid &&
            TryComp<AntagSelectionComponent>(ruleUid, out var antag))
        {
            TryMarkRuleAdded(ruleUid);
            _gameTicker.StartGameRule(ruleUid);

            var players = _antag.GetActivePlayerCount();
            foreach (var session in sessions)
            {
                // ignoreExclusivity: true - an enrolling ghost may already be an antag. Bans/validity still apply.
                _antag.TryAssignNextAvailableAntag((ruleUid, antag), session, players, checkPref: false, ignoreExclusivity: true);
            }

            return;
        }

        FireFallbackRule(ent);
        TryQueueDel(rule);
    }

    /// <summary>
    /// Where this event's antag spawns, asked of the rule the same way antag selection does. Resolved once
    /// when the rule's added (see <see cref="OnVoteSpawned"/>) and stored on the component as the warp target.
    /// </summary>
    private MapCoordinates? GetWarpTarget(Entity<AntagSelectionComponent> rule)
    {
        foreach (var selector in rule.Comp.Antags)
        {
            if (!_proto.TryIndex(selector.Proto, out var def))
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

      var net = GetNetEntity(attached);
      foreach (var voteUid in voteManager.VoteEntities)
      {
          if (TryComp<MoffEnrollEventComponent>(voteUid, out var enroll))
              return enroll.RandomPick.Contains(net);
      }

      return false;
    }
}
