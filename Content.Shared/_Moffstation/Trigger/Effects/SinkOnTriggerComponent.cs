using Content.Shared.DeviceLinking;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Trigger.Effects;

/// <summary>
/// Send a signal to the indicated sink when the entity is triggered
/// </summary>
/// <remarks>
/// Sadly SignalOnTriggerComponent is already taken.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SinkOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The port that gets signaled when the switch turns on.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> Port = "Trigger";
}
