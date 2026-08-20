using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Trigger.Components.Effects;

/// <summary>
/// This component fires <see cref="BaseTriggerOnXComponent.KeyOut"/> when its entity is toggled on.
/// The user is the toggling event's user.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnToggleOnComponent : BaseTriggerOnXComponent;

/// <summary>
/// This component fires <see cref="BaseTriggerOnXComponent.KeyOut"/> when its entity is toggled off.
/// The user is the toggling event's user.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnToggleOffComponent : BaseTriggerOnXComponent;
