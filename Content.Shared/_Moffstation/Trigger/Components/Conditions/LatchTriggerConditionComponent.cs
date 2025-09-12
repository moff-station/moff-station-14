using Content.Shared._Moffstation.Trigger.EntitySystems;
using Content.Shared.Trigger.Components.Conditions;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Trigger.Components.Conditions;

/// <summary>
/// This <see cref="BaseTriggerConditionComponent">trigger condition</see> acts as a latch, permitting triggering once
/// before entering into a "closed" state which disallows further triggers until it is reset.
/// This component also adds a verb to reset the latch.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(LatchTriggerConditionSystem))]
public sealed partial class LatchTriggerConditionComponent : BaseTriggerConditionComponent
{
    [DataField(required: true)]
    public LocId ResetVerbName;

    [DataField(required: true)]
    public LocId ResetVerbMessage;

    /// <summary>
    /// The message to use for the verb when this latch cannot be reset and is triggerable.
    /// </summary>
    [DataField(required: true)]
    public LocId AlreadyResetMessage;

    /// <summary>
    /// The state of this latch, indicating if the trigger is triggerable.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool Triggerable = true;

    public LocId Message => Triggerable ? AlreadyResetMessage : ResetVerbMessage;
}
