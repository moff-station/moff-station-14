using Content.Shared.Actions;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Shared._Moffstation.Silicons.StationAi;

/// Event raised on <see cref="AiShellControllerComponent"/>s when they start controlling an <see cref="AiShellComponent"/>.
[ByRefEvent]
public readonly record struct AiShellControlStartedEvent(
    Entity<AiShellControllerComponent> Controller,
    Entity<AiShellComponent> Shell,
    Entity<BorgChassisComponent>? Holder
);

/// Event raised on <see cref="AiShellComponent"/>s when they start being controlled by an <see cref="AiShellControllerComponent"/>.
[ByRefEvent]
public readonly record struct AiShellStartedBeingControlledEvent(
    Entity<AiShellControllerComponent> Controller,
    Entity<AiShellComponent> Shell,
    Entity<BorgChassisComponent>? Holder
);

/// Event raised on <see cref="AiShellControllerComponent"/>s when they stop controlling an <see cref="AiShellComponent"/>.
[ByRefEvent]
public readonly record struct AiShellControlStoppedEvent(
    Entity<AiShellControllerComponent> Controller,
    Entity<AiShellComponent> Shell,
    Entity<BorgChassisComponent>? Holder
);

/// Event raised on <see cref="AiShellComponent"/>s when they stop being controlled by an <see cref="AiShellControllerComponent"/>.
[ByRefEvent]
public readonly record struct AiShellStoppedBeingControlledEvent(
    Entity<AiShellControllerComponent> Controller,
    Entity<AiShellComponent> Shell,
    Entity<BorgChassisComponent>? Holder
);

/// Event raised on <see cref="AiShellHolderComponent"/>s its contained <see cref="AiShellComponent"/> starts being
/// controlled by an <see cref="AiShellComponent"/>.
/// This event is also raised when an already-controlled shell is inserted into a chassis.
[ByRefEvent]
public readonly record struct ContainedAiShellStoppedBeingControlledEvent(
    Entity<AiShellControllerComponent> Controller,
    Entity<AiShellComponent> Shell,
    Entity<BorgChassisComponent> Holder
);

/// Event raised on <see cref="AiShellHolderComponent"/>s its contained <see cref="AiShellComponent"/> stops being
/// controlled by an <see cref="AiShellComponent"/>.
/// This event is also raised when a shell is removed from an already-controlled chassis.
[ByRefEvent]
public readonly record struct ContainedAiShellStartedBeingControlledEvent(
    Entity<AiShellControllerComponent> Controller,
    Entity<AiShellComponent> Shell,
    Entity<BorgChassisComponent> Holder
);

/// Raised on and by a <see cref="AiShellControllerComponent"/> to open/close its AI shell control UI.
public sealed partial class ToggleAiShellControllerUiEvent : InstantActionEvent;

/// Raised on and by a <see cref="ControlledAiShellComponent"/> to terminate an AI shell control session.
public sealed partial class StopAiShellControlEvent : InstantActionEvent;
