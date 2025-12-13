using Robust.Shared.GameStates;

namespace Content.Shared._ES.Objectives.Components;

/// <summary>
/// Denotes an entity that can have objectives associated with it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedObjectiveSystem), Other = AccessPermissions.None)]
public sealed partial class ESObjectiveHolderComponent : Component
{
    /// <summary>
    /// The complete set of objectives that can be viewed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> Objectives = [];

    /// <summary>
    /// Objectives created for and assigned to this entity primarily.
    /// Subset of <see cref="Objectives"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> OwnedObjectives = [];
}

/// <summary>
/// Event raised on <see cref="ESObjectiveHolderComponent"/> when their objectives change.
/// </summary>
[ByRefEvent]
public readonly record struct ESObjectivesChangedEvent(List<EntityUid> Objectives, List<EntityUid> Added, List<EntityUid> Removed);

/// <summary>
/// Event raised on an objective holder to return any additional objectives they have from other sources.
/// </summary>
[ByRefEvent]
public record struct ESGetAdditionalObjectivesEvent(Entity<ESObjectiveHolderComponent> Holder, List<Entity<ESObjectiveComponent>> Objectives);

/// <summary>
/// Event raised when an objective's progress changes
/// </summary>
[ByRefEvent]
public readonly record struct ESObjectiveProgressChangedEvent(Entity<ESObjectiveComponent> Objective, float OldProgress, float NewProgress);
