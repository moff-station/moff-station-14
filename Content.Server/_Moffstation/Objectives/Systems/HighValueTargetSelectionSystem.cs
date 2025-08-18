using Content.Server._Moffstation.Objectives.Components;
using Content.Server.Preferences.Managers;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.Objectives.Systems;

/// <summary>
/// This handles selecting a high-value target for any systems that select random targets.
/// </summary>
public sealed class HighValueTargetSelectionSystem : EntitySystem
{
    [Dependency] private readonly IServerPreferencesManager _pref = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Selects a target from the list of possible targets with a priority on those with
    /// the High Value Target role selected.
    /// </summary>
    public Entity<MindComponent> SelectTarget(
        Entity<HighValueTargetSelectionComponent?> rule,
        IEnumerable<Entity<MindComponent>> targets)
    {
        var allPossibleTargets = new List<Entity<MindComponent>>(targets);
        if (!Resolve(rule.Owner, ref rule.Comp))
            return _random.Pick(allPossibleTargets);

        _random.Shuffle(allPossibleTargets);
        var targetQueue = new Queue<Entity<MindComponent>>(allPossibleTargets);

        while (targetQueue.Count > 1)
        {
            var mind = targetQueue.Dequeue();
            if (mind.Comp.UserId is not { } userId)
                continue;

            var pref = (HumanoidCharacterProfile) _pref.GetPreferences(userId).SelectedCharacter;
            if (pref.AntagPreferences.Contains(rule.Comp.HighValueTargetPrototype)
                || _random.Prob(rule.Comp.NonTargetSelectionProbability))
                return mind;
        }

        return targetQueue.Peek();
    }
}
