using Content.Shared.EntityEffects;
using Content.Shared.Inventory;
using Content.Shared.Trigger.Components.Conditions;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Trigger.Components.Conditions;

/// <summary>
/// This <see cref="BaseTriggerConditionComponent">trigger condition</see> permits a trigger if all of its
/// <see cref="Conditions"/> pass. This allows for rich, dynamic behavior to be defined entirely within yaml.
/// </summary>
public abstract partial class BaseEntityEffectTriggerConditionComponent : BaseTriggerConditionComponent
{
    [DataField(required: true)]
    public List<EntityEffectCondition> Conditions = [];
}

/// <summary>
/// This <see cref="BaseEntityEffectTriggerConditionComponent"/> runs its conditions with the subject entity being this
/// component's owner.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EntityEffectTriggerConditionComponent : BaseEntityEffectTriggerConditionComponent;

/// <summary>
/// This <see cref="BaseEntityEffectTriggerConditionComponent"/> runs its conditions with the subject entity being the
/// entity which has this component's owner equipped in <see cref="Slots"/>. If the owner is not equipped by any entity,
/// or is equipped in a slot which is not in <see cref="Slots"/>, the condition immediately fails.
/// Basically, you put this condition on some clothes and the conditions use the wearer entity as the subject.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EquipeeEntityEffectTriggerConditionComponent :
    BaseEntityEffectTriggerConditionComponent
{
    [DataField]
    public SlotFlags Slots = SlotFlags.WITHOUT_POCKET;
}

/// <summary>
/// This <see cref="BaseEntityEffectTriggerConditionComponent"/> runs its conditions with the subject entity being the
/// entity which is holding this component's owner. If the owner is not held by any entity, the condition immediately
/// fails.
/// Basically, you put this condition on an item and the conditions use the holding entity as the subject.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HolderEntityEffectTriggerConditionComponent :
    BaseEntityEffectTriggerConditionComponent;
