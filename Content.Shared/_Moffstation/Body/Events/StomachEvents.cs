using Content.Shared.Chemistry.Components;

namespace Content.Shared._Moffstation.Body.Events;

[ByRefEvent]
public record struct GetStomachContentsEvent(SolutionComponent Contents, bool Handled = false);

[ByRefEvent]
public record struct ApplyStomachContentsEvent(SolutionComponent Contents);

[ByRefEvent]
public record struct EmptyStomachEvent;
