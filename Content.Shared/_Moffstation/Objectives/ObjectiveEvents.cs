namespace Content.Shared._Moffstation.Objectives;

/// <summary>
/// Event raised on an objective when it is added to a mind
/// </summary>
[ByRefEvent]
public record struct ObjectiveAddedEvent(EntityUid Mind);

/// <summary>
/// Event raised on an objective when it is removed from a mind
/// </summary>
[ByRefEvent]
public record struct ObjectiveRemovedEvent(EntityUid Mind);
