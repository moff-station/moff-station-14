using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Objectives.Components;

/// <summary>
/// Denotes a general objective that is associated with a <see cref="ESObjectiveHolderComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedObjectiveSystem), Other = AccessPermissions.None)]
[EntityCategory("Objectives")]
public sealed partial class ESObjectiveComponent : Component
{
    /// <summary>
    /// Current progress on the objective on the interval [0, 1]
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Progress;

    /// <summary>
    /// Icon displayed for this objective in the UI.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Icon;
}

/// <summary>
/// Event raised on an objective to calculate its current progress on the interval [0, 1].
/// </summary>
[ByRefEvent]
public record struct ESGetObjectiveProgressEvent(float Progress = 0);

/// <summary>
/// Event raised after an objective is created in order to initialize its data
/// </summary>
[ByRefEvent]
public readonly record struct ESInitializeObjectiveEvent(Entity<ESObjectiveHolderComponent> Holder);

/// <summary>
/// Event raised on an objective after it's been received by a given mind.
/// </summary>
[ByRefEvent]
public readonly record struct ESObjectiveAddedEvent(EntityUid Holder, EntityUid Objective);

/// <summary>
/// Event raised on an objective after it's been removed from a given mind.
/// </summary>
[ByRefEvent]
public readonly record struct ESObjectiveRemovedEvent(EntityUid Holder, EntityUid Objective);
