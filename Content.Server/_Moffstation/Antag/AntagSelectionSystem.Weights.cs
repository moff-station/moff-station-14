using System.Collections.Generic;
using System.Linq;
using Content.Server._Moffstation.Antag;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

// ReSharper disable once CheckNamespace
namespace Content.Server.Antag;

/// <summary>
/// Moff - Players who don't get selected to be antag get an extra name in the hat for future rounds
/// This continues stacking until they roll antag, to which they will get reset to a weight of 1
/// </summary>
public sealed partial class AntagSelectionSystem
{
    [Dependency] private IWeightedAntagManager _weightedAntagMan = default!;

    private void InitializeAntagWeights()
    {
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    /// <summary>
    /// Check who was and wasn't an antag at the end of the round
    /// This also checks preferences, if it's a nukie round, you don't get a weight if you didn't have nukie enabled
    /// </summary>
    private void OnRunLevelChanged(GameRunLevelChangedEvent args)
    {
        if (args.New != GameRunLevel.PostRound)
            return;

        var antagRoleList = new List<ProtoId<AntagPrototype>>();
        var antagUsers = new HashSet<NetUserId>();
        var rules = QueryAllRules();
        while (rules.MoveNext(out var uid, out var comp, out _))
        {
            // Check if the rule is exempt from changing weights
            if (HasComp<MoffUnweightedAntagComponent>(uid))
                continue;

            foreach (var antag in comp.Antags)
            {
                if (ProtoMan.Resolve(antag.Proto, out var def))
                    antagRoleList.AddRange(def.PrefRoles);
            }

            // Get the UserIds of the players who were antags due to the rules in question
            foreach (var (mindId, _) in comp.AssignedMinds.Values.SelectMany(minds => minds))
            {
                if (TryComp<MindComponent>(mindId, out var mind) && mind.UserId is { } userId)
                    antagUsers.Add(userId);
            }
        }

        // If they were found in one of the rules, back down to 1
        foreach (var userId in antagUsers)
        {
            _weightedAntagMan.SetWeight(userId, 1);
        }

        // Look through all the other players
        foreach (var session in _playerManager.Sessions)
        {
            // Check if they're actually in round.
            if (!GameTicker.UserHasJoinedGame(session))
                continue;

            if (antagUsers.Contains(session.UserId))
                continue;

            // Only increment if their preferences match
            if (TryGetValidAntagPreferences(session, antagRoleList))
                _weightedAntagMan.IncrementWeight(session.UserId);
        }
    }

    // Persist antag weights at round-end.
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        _ = _weightedAntagMan.Save();
    }
}
