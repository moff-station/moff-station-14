using System.Collections.Generic;
using System.Linq;
using Content.Server._Moffstation.Antag;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

// ReSharper disable once CheckNamespace
namespace Content.Server.Antag;

/// <summary>
/// Moff - Players who dont' get selected to be antag get an extra name in the hat for future rounds
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

        // Walk the weighted (non-exempt) antag rules that ran this round. Gather the antag preferences they handed out,
        // and consume the weight of anyone who actually rolled one of them.
        var distributed = new List<ProtoId<AntagPrototype>>();
        var rules = QueryAllRules();
        while (rules.MoveNext(out var uid, out var comp, out _))
        {
            // Rules marked MoffUnweightedAntag opt out of weighting entirely - their antags neither accrue nor consume.
            if (HasComp<MoffUnweightedAntagComponent>(uid))
                continue;

            foreach (var antag in comp.Antags)
            {
                if (ProtoMan.Resolve(antag.Proto, out var def))
                    distributed.AddRange(def.PrefRoles);
            }

            // Rolled a weighted antag, so their accrued weight is spent - back down to 1.
            foreach (var session in comp.PreSelectedSessions.Values.SelectMany(sessions => sessions))
            {
                if (_role.PlayerIsAntagonist(session))
                    _weightedAntagMan.SetWeight(session.UserId, 1);
            }
        }

        // Everyone who didn't roll an antag but opted into one that actually ran gets another entry in the hat.
        foreach (var session in _playerManager.Sessions)
        {
            if (_role.PlayerIsAntagonist(session))
                continue;

            if (TryGetValidAntagPreferences(session, distributed))
                _weightedAntagMan.IncrementWeight(session.UserId);
        }
    }

    // Persist antag weights at round-end.
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        _ = _weightedAntagMan.Save();
    }
}
