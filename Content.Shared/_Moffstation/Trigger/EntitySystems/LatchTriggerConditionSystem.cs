using Content.Shared._Moffstation.Trigger.Components.Conditions;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Systems;
using Content.Shared.Verbs;

namespace Content.Shared._Moffstation.Trigger.EntitySystems;

/// <summary>
/// This system implements the behavior of <see cref="LatchTriggerConditionComponent"/> by handling trigger events and
/// providing the latch's reset verb.
/// </summary>
public sealed partial class LatchTriggerConditionSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<LatchTriggerConditionComponent, AttemptTriggerEvent>(OnAttemptTrigger,
            before: [typeof(TriggerSystem)]);
        SubscribeLocalEvent<LatchTriggerConditionComponent, TriggerEvent>(OnTrigger);
        SubscribeLocalEvent<LatchTriggerConditionComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
    }

    private static void OnAttemptTrigger(Entity<LatchTriggerConditionComponent> entity, ref AttemptTriggerEvent args)
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

    private void OnGetVerbs(Entity<LatchTriggerConditionComponent> entity,
        ref GetVerbsEvent<ActivationVerb> args)
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
