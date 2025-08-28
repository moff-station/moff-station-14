using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Conditions;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Trigger.Systems;
using Content.Shared.Verbs;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation;

public abstract partial class BaseEntityEffectTriggerConditionComponent : BaseTriggerConditionComponent
{
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    // ReSharper disable once CollectionNeverUpdated.Global
    [DataField(required: true)]
    public List<EntityEffectCondition> Conditions = [];
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EntityEffectTriggerConditionComponent : BaseEntityEffectTriggerConditionComponent;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EquipeeEntityEffectTriggerConditionComponent : BaseEntityEffectTriggerConditionComponent
{
    [DataField]
    public SlotFlags Slots = SlotFlags.WITHOUT_POCKET;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HolderEntityEffectTriggerConditionComponent : BaseEntityEffectTriggerConditionComponent;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnDamageChangedComponent : BaseTriggerOnXComponent;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnEquipeeDamageChangedComponent : BaseTriggerOnXComponent;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnHolderDamageChangedComponent : BaseTriggerOnXComponent;

public sealed partial class TriggerOnDamageChangedSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TriggerOnDamageChangedComponent, DamageChangedEvent>(OnDamageChangedGeneric);
        SubscribeLocalEvent<TriggerOnEquipeeDamageChangedComponent, InventoryRelayedEvent<DamageChangedEvent>>(
            OnDamageChangedGeneric);
        SubscribeLocalEvent<TriggerOnHolderDamageChangedComponent, HeldRelayedEvent<DamageChangedEvent>>(
            OnDamageChangedGeneric);
    }

    private void OnDamageChangedGeneric<TComp, TArgs>(Entity<TComp> entity, ref TArgs args)
        where TComp : BaseTriggerOnXComponent
    {
        _trigger.Trigger(entity, null, entity.Comp.KeyOut);
    }
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LatchTriggerConditionComponent : BaseTriggerConditionComponent
{
    [DataField(required: true)]
    public LocId ResetVerbName;

    [DataField(required: true)]
    public LocId ResetVerbMessage;

    [DataField(required: true)]
    public LocId AlreadyResetMessage;

    [ViewVariables, AutoNetworkedField]
    public bool Triggerable = true;

    public LocId Message => Triggerable ? AlreadyResetMessage : ResetVerbMessage;
}

public sealed partial class TriggerLatchSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<LatchTriggerConditionComponent, AttemptTriggerEvent>(OnAttemptTrigger,
            before: [typeof(TriggerSystem)]);
        SubscribeLocalEvent<LatchTriggerConditionComponent, TriggerEvent>(OnTrigger);
        SubscribeLocalEvent<LatchTriggerConditionComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
    }

    private void OnAttemptTrigger(Entity<LatchTriggerConditionComponent> entity, ref AttemptTriggerEvent args)
    {
        if (args.Key == null ||
            entity.Comp.Keys.Contains(args.Key))
            args.Cancelled |= !entity.Comp.Triggerable;
    }

    private void OnTrigger(Entity<LatchTriggerConditionComponent> entity, ref TriggerEvent args)
    {
        if (args.Key != null &&
            !entity.Comp.Keys.Contains(args.Key))
            return;

        entity.Comp.Triggerable = false;
        Dirty(entity);
    }

    private void OnGetVerbs(Entity<LatchTriggerConditionComponent> entity, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess ||
            !args.CanInteract ||
            !args.CanComplexInteract)
            return;

        args.Verbs.Add(new ActivationVerb
        {
            Act = () => Reset(entity),
            Disabled = entity.Comp.Triggerable,
            DoContactInteraction = true,
            Text = Loc.GetString(entity.Comp.ResetVerbName),
            Message = Loc.GetString(entity.Comp.Message),
        });
    }

    private void Reset(Entity<LatchTriggerConditionComponent> entity)
    {
        entity.Comp.Triggerable = true;
        Dirty(entity);
    }
}
