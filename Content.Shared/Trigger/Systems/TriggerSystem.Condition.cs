using Content.Shared.Trigger.Components.Conditions;
using Content.Shared.Verbs;
using Robust.Shared.Random;

// Moffstation - Start
using Content.Shared._Moffstation.Trigger.Components.Conditions;
using Content.Shared.EntityEffects;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Random.Helpers;
// Moffstation - End

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{
    // Moffstation - Start
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    // Moffstation - End

    private void InitializeCondition()
    {
        SubscribeLocalEvent<WhitelistTriggerConditionComponent, AttemptTriggerEvent>(OnWhitelistTriggerAttempt);
        SubscribeLocalEvent<UseDelayTriggerConditionComponent, AttemptTriggerEvent>(OnUseDelayTriggerAttempt);
        SubscribeLocalEvent<ToggleTriggerConditionComponent, AttemptTriggerEvent>(OnToggleTriggerAttempt);
        SubscribeLocalEvent<RandomChanceTriggerConditionComponent, AttemptTriggerEvent>(OnRandomChanceTriggerAttempt);
        SubscribeLocalEvent<MindRoleTriggerConditionComponent, AttemptTriggerEvent>(OnMindRoleTriggerAttempt);

        SubscribeLocalEvent<ToggleTriggerConditionComponent, GetVerbsEvent<AlternativeVerb>>(OnToggleGetAltVerbs);

        // Moffstation - Start - EntityTriggerCondition components
        SubscribeLocalEvent<EntityEffectTriggerConditionComponent, AttemptTriggerEvent>(OnEntityEffectTriggerAttempt);
        SubscribeLocalEvent<EquipeeEntityEffectTriggerConditionComponent, AttemptTriggerEvent>(
            OnEquipeeEntityEffectTriggerAttempt);
        SubscribeLocalEvent<HolderEntityEffectTriggerConditionComponent, AttemptTriggerEvent>(
            OnHolderEntityEffectTriggerAttempt);
        // Moffstation - End
    }

    private void OnWhitelistTriggerAttempt(Entity<WhitelistTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.Keys.Contains(args.Key))
            return;

        args.Cancelled |= !_whitelist.CheckBoth(args.User, ent.Comp.UserBlacklist, ent.Comp.UserWhitelist);
    }

    private void OnUseDelayTriggerAttempt(Entity<UseDelayTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.Keys.Contains(args.Key))
            return;

        args.Cancelled |= _useDelay.IsDelayed(ent.Owner, ent.Comp.UseDelayId);
    }

    private void OnToggleTriggerAttempt(Entity<ToggleTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.Keys.Contains(args.Key))
            return;

        args.Cancelled |= !ent.Comp.Enabled;
    }

    private void OnToggleGetAltVerbs(Entity<ToggleTriggerConditionComponent> ent,
        ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString(ent.Comp.ToggleVerb),
            Act = () => Toggle(ent, user)
        });
    }

    private void Toggle(Entity<ToggleTriggerConditionComponent> ent, EntityUid user)
    {
        var msg = ent.Comp.Enabled ? ent.Comp.ToggleOff : ent.Comp.ToggleOn;
        _popup.PopupPredicted(Loc.GetString(msg), ent.Owner, user);
        ent.Comp.Enabled = !ent.Comp.Enabled;
        Dirty(ent);
    }

    private void OnRandomChanceTriggerAttempt(Entity<RandomChanceTriggerConditionComponent> ent,
        ref AttemptTriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.Keys.Contains(args.Key))
            return;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var hash = new List<int>
        {
            (int)_timing.CurTick.Value,
            GetNetEntity(ent).Id,
            args.User == null ? 0 : GetNetEntity(args.User.Value).Id,
        };
        var seed = SharedRandomExtensions.HashCodeCombine(hash);
        var rand = new System.Random(seed);

        args.Cancelled |= !rand.Prob(ent.Comp.SuccessChance); // When not successful, Cancelled = true
    }
    private void OnMindRoleTriggerAttempt(Entity<MindRoleTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.Keys.Contains(args.Key))
            return;

        if (ent.Comp.EntityWhitelist != null)
        {
            if (!_mind.TryGetMind(ent.Owner, out var entMindId, out var entMindComp))
            {
                args.Cancelled = true; // the entity has no mind
                return;
            }
            if (!_role.MindHasRole((entMindId, entMindComp), ent.Comp.EntityWhitelist))
            {
                args.Cancelled = true; // the entity does not have the required role
                return;
            }
        }

        if (ent.Comp.UserWhitelist != null)
        {
            if (args.User == null || !_mind.TryGetMind(args.User.Value, out var userMindId, out var userMindComp))
            {
                args.Cancelled = true; // no user or the user has no mind
                return;
            }
            if (!_role.MindHasRole((userMindId, userMindComp), ent.Comp.UserWhitelist))
            {
                args.Cancelled = true; // the user does not have the required role
            }
        }
    }

    // Moffstation - Start - EntityEffectTriggerCondition components
    private void OnEntityEffectTriggerAttempt(
        Entity<EntityEffectTriggerConditionComponent> ent,
        ref AttemptTriggerEvent args
    )
    {
        HandleEntityEffectTriggerAttempt(ent, ent, ref args);
    }

    private void OnEquipeeEntityEffectTriggerAttempt(
        Entity<EquipeeEntityEffectTriggerConditionComponent> ent,
        ref AttemptTriggerEvent args
    )
    {
        if (!_inventory.InSlotWithFlags(ent.Owner, ent.Comp.Slots) ||
            !TryComp(ent, out TransformComponent? xform))
        {
            args.Cancelled = true;
            return;
        }

        HandleEntityEffectTriggerAttempt(xform.ParentUid, ent, ref args);
    }

    private void OnHolderEntityEffectTriggerAttempt(
        Entity<HolderEntityEffectTriggerConditionComponent> ent,
        ref AttemptTriggerEvent args
    )
    {
        if (!TryComp(ent, out TransformComponent? xform) ||
            !_hands.IsHolding(xform.ParentUid, ent))
        {
            args.Cancelled = true;
            return;
        }

        HandleEntityEffectTriggerAttempt(xform.ParentUid, ent, ref args);
    }

    private void HandleEntityEffectTriggerAttempt(
        EntityUid entity,
        BaseEntityEffectTriggerConditionComponent conditionComp,
        ref AttemptTriggerEvent args
    )
    {
        if (args.Key != null &&
            !conditionComp.Keys.Contains(args.Key))
            args.Cancelled = true;

        var entityEffectBaseArgs = new EntityEffectBaseArgs(entity, EntityManager);
        foreach (var condition in conditionComp.Conditions)
        {
            if (!condition.Condition(entityEffectBaseArgs))
            {
                args.Cancelled = true;
                break;
            }
        }
    }
    // Moffstation - End
}
