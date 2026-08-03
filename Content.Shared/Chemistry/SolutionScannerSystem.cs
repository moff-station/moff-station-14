using Content.Shared.Chemistry.Components;
using Content.Shared.Clothing;
using Content.Shared.Inventory;

namespace Content.Shared.Chemistry;

public sealed class SolutionScannerSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<SolutionScannerComponent, SolutionScanEvent>(OnSolutionScanAttempt);
        SubscribeLocalEvent<SolutionScannerComponent, InventoryRelayedEvent<SolutionScanEvent>>((e, c, ev) => OnSolutionScanAttempt(e, c, ev.Args));

        SubscribeLocalEvent<SolutionScannerComponent, ItemMaskToggledEvent>(OnItemMaskToggled); // Moffstation - Toggling masks toggles associated solution scanners
    }

    private void OnSolutionScanAttempt(EntityUid eid, SolutionScannerComponent component, SolutionScanEvent args)
    {
        args.CanScan = component.Enabled; // Moffstation - Solution scanner can be enabled/disabled.
    }

    // Moffstation - Begin - Toggling masks toggles associated solution scanners
    private void OnItemMaskToggled(Entity<SolutionScannerComponent> entity, ref ItemMaskToggledEvent args)
    {
        entity.Comp.Enabled = !args.Mask.Comp.IsToggled;
        Dirty(entity);
    }
    // Moffstation - End
}

public sealed class SolutionScanEvent : EntityEventArgs, IInventoryRelayEvent
{
    public bool CanScan;
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET; // Moffstation - Solution scanning from any clothing slot
}
