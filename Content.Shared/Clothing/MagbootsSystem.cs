using Content.Shared._Moffstation.Clothing; // Moffstation
using Content.Shared._Moffstation.Weapons.Ranged.Components; // Moffstation
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Clothing;

public sealed class SharedMagbootsSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!; // Moffstation

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagbootsComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<MagbootsComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<MagbootsComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<MagbootsComponent, IsWeightlessEvent>(OnIsWeightless);
        SubscribeLocalEvent<MagbootsComponent, InventoryRelayedEvent<IsWeightlessEvent>>(OnIsWeightless);
        SubscribeLocalEvent<MagbootsComponent, InventoryRelayedEvent<RecoilKickAttemptEvent>>(OnRecoilKickAttempt); // Moffstation
        SubscribeLocalEvent<MagbootsComponent, ToggleMagbootsActionEvent>(OnMagbootsToggled); // Moffstation
    }

    private void OnToggled(Entity<MagbootsComponent> ent, ref ItemToggledEvent args)
    {
        if (_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
            UpdateMagbootEffects(container.Owner, ent, args.Activated);
    }

    // Moffstation - Start
    private void OnMagbootsToggled(Entity<MagbootsComponent> ent, ref ToggleMagbootsActionEvent args)
    {
        args.Handled = true;

        _actions.SetToggled(args.Action.Owner, !args.Action.Comp.Toggled);
        ent.Comp.EffectActive = args.Action.Comp.Toggled;

        UpdateMagbootEffects(ent.Owner, ent, args.Action.Comp.Toggled);
    }
    // Moffstation - End

    private void OnGotUnequipped(Entity<MagbootsComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        UpdateMagbootEffects(args.Wearer, ent, false);
    }

    private void OnGotEquipped(Entity<MagbootsComponent> ent, ref ClothingGotEquippedEvent args)
    {
        UpdateMagbootEffects(args.Wearer, ent, _toggle.IsActivated(ent.Owner));
    }

    public void UpdateMagbootEffects(EntityUid user, Entity<MagbootsComponent> ent, bool state)
    {
        // TODO: public api for this and add access
        if (TryComp<MovedByPressureComponent>(user, out var moved))
            moved.Enabled = !state;

        _gravity.RefreshWeightless(user);

        if (state)
            _alerts.ShowAlert(user, ent.Comp.MagbootsAlert);
        else
            _alerts.ClearAlert(user, ent.Comp.MagbootsAlert);
    }

    private void OnIsWeightless(Entity<MagbootsComponent> ent, ref IsWeightlessEvent args)
    {
        // Moffstation - start - modified to accomodate borg toggleable magboots
        if (args.Handled ||
            ent.Comp.UseGenericToggle && !_toggle.IsActivated(ent.Owner) ||
            ent.Comp is { UseGenericToggle: false, EffectActive: false })
            return;
        // Moffstation - end

        // do not cancel weightlessness if the person is in off-grid.
        if (ent.Comp.RequiresGrid && !_gravity.EntityOnGravitySupportingGridOrMap(ent.Owner))
            return;

        args.IsWeightless = false;
        args.Handled = true;
    }

    private void OnIsWeightless(Entity<MagbootsComponent> ent, ref InventoryRelayedEvent<IsWeightlessEvent> args)
    {
        OnIsWeightless(ent, ref args.Args);
    }

    // Moffstation - Start
    private void OnRecoilKickAttempt(
        Entity<MagbootsComponent> ent,
        ref InventoryRelayedEvent<RecoilKickAttemptEvent> args
    )
    {
        if (!_toggle.IsActivated(ent.Owner))
            return;

        // Do not modify kick effects if the entity is off-grid.
        if (ent.Comp.RequiresGrid && !_gravity.EntityOnGravitySupportingGridOrMap(ent.Owner))
            return;

        // Magboots fully mitigate the kick.
        args.Args.ImpulseEffectivenessFactor *= 0.0f;
    }
    // Moffstation - End
}
