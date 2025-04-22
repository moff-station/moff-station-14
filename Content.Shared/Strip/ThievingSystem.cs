using Content.Shared.Alert; // Moffstation - Import alert so we can toggle
using Content.Shared.Inventory;
// using Content.Shared.Strip;  // Moffstation - Remove unused import
using Content.Shared.Strip.Components;

namespace Content.Shared.Strip;

public sealed class ThievingSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThievingComponent, BeforeStripEvent>(OnBeforeStrip);
        SubscribeLocalEvent<ThievingComponent, InventoryRelayedEvent<BeforeStripEvent>>((e, c, ev) => OnBeforeStrip(e, c, ev.Args));
        // Moffstation - Start - Event listeners for toggling stealth and for initializing/removing the alert
        SubscribeLocalEvent<ThievingComponent, ToggleThievingEvent>(OnToggleStealthy);
        SubscribeLocalEvent<ThievingComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<ThievingComponent, ComponentRemove>(OnCompRemoved);
        // Moffstation - End
    }

    private void OnBeforeStrip(EntityUid uid, ThievingComponent component, BeforeStripEvent args)
    {
        args.Stealth |= component.Stealthy;
        if (args.Stealth)   // Moffstation - Allow disabling stealth
            args.Additive -= component.StripTimeReduction;
    }

    // Moffstation - Start - Add function for toggling stealth, and the function to initialize/remove the alert
    private void OnCompInit(EntityUid uid, ThievingComponent comp, ComponentInit args)
    {
        _alertsSystem.ShowAlert(uid, comp.StealthyAlertProtoId, 1);
    }

    private void OnCompRemoved(EntityUid uid, ThievingComponent comp, ComponentRemove args)
    {
        _alertsSystem.ClearAlert(uid, comp.StealthyAlertProtoId);
    }

    private void OnToggleStealthy(Entity<ThievingComponent> ent, ref ToggleThievingEvent args)
    {
        if(args.Handled)
            return;

        ent.Comp.Stealthy = !ent.Comp.Stealthy;

        switch (ent.Comp.Stealthy)
        {
            case false:
                _alertsSystem.ShowAlert(ent.Owner, ent.Comp.StealthyAlertProtoId, 0);
                break;

            case true:
                _alertsSystem.ShowAlert(ent.Owner, ent.Comp.StealthyAlertProtoId, 1);
                break;
        }
        args.Handled = true;
    }
    //Moffstation - End
}
