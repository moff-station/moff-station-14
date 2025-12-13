using Content.Shared._ES.Objectives;
using Content.Shared.Mind;

namespace Content.Server._ES.Masks.Objectives.Relays;

/// <summary>
///     This provides a base class for mind relays and handles raising events on both the mind, and its objectives.
/// </summary>
public abstract class ESBaseMindRelay : EntitySystem
{
    [Dependency] private readonly ESSharedObjectiveSystem _objective = default!;

    /// <summary>
    ///     Raises the given by-ref event on the mind, and all of its objectives.
    /// </summary>
    public void RaiseMindEvent<TEvent>(Entity<MindComponent> mind, ref TEvent ev) where TEvent : notnull
    {
        RaiseLocalEvent(mind, ref ev);

        foreach (var objective in _objective.GetObjectives(mind.Owner))
        {
            RaiseLocalEvent(objective, ref ev);
        }
    }

    /// <summary>
    ///     Raises the given by-value event on the mind, and all of its objectives.
    /// </summary>
    public void RaiseMindEvent<TEvent>(Entity<MindComponent> mind, TEvent ev) where TEvent : notnull
    {
        RaiseLocalEvent(mind, ev);

        foreach (var objective in _objective.GetObjectives(mind.Owner))
        {
            RaiseLocalEvent(objective, ev);
        }
    }
}
