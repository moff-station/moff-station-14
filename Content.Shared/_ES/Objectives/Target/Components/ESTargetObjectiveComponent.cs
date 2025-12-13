using Content.Shared._ES.Objectives.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Objectives.Target.Components;

/// <summary>
/// General component that manages objectives which target a given entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESTargetObjectiveSystem), Other = AccessPermissions.None)]
public sealed partial class ESTargetObjectiveComponent : Component
{
    [DataField]
    public EntityUid? Target;

    [DataField]
    public LocId? Title;
}

/// <summary>
/// Event raised on an objective to return all potential candidates. Note that this has no real filtering on it.
/// </summary>
[ByRefEvent]
public record struct ESGetObjectiveTargetCandidates(Entity<ESObjectiveHolderComponent> Holder, List<EntityUid> Candidates);

/// <summary>
/// Event raised on an objective entity to check if a given
/// </summary>
[ByRefEvent]
public record struct ESValidateObjectiveTargetCandidates(Entity<ESObjectiveHolderComponent> Holder, EntityUid Candidate)
{
    public readonly Entity<ESObjectiveHolderComponent> Holder = Holder;
    public readonly EntityUid Candidate = Candidate;
    public bool Valid { get; private set; } = true;

    public void Invalidate()
    {
        Valid = false;
    }
}
