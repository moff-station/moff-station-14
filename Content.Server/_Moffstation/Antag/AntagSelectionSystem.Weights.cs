using System.Collections.Generic;
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

        // Gather the antag preferences distributed by the weighted antag rules that ran this round.
        var distributed = new List<ProtoId<AntagPrototype>>();
        var rules = QueryAllRules();
        while (rules.MoveNext(out _, out var comp, out _))
        {
            if (!comp.UseWeights)
                continue;

            foreach (var antag in comp.Antags)
            {
                if (ProtoMan.Resolve(antag.Proto, out var def))
                    distributed.AddRange(def.PrefRoles);
            }
        }

        // No weighted antag ran, so nobody's weight should change.
        if (distributed.Count == 0)
            return;

        foreach (var session in _playerManager.Sessions)
        {
            // Players who rolled antag this round have their accrued weight "consumed" back down to 1.
            if (_role.PlayerIsAntagonist(session))
            {
                _weightedAntagMan.SetWeight(session.UserId, 1);
                continue;
            }

            // Only players who opted into an antag that actually ran (and aren't banned/lacking playtime) accrue weight.
            if (!TryGetValidAntagPreferences(session, distributed))
                continue;

            _weightedAntagMan.IncrementWeight(session.UserId);
        }
    }

    // Persist antag weights at round-end.
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        _ = _weightedAntagMan.Save();
    }
}
