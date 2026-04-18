using System.Text;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Clothing;

public sealed class ExtraExaminableSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExtraExaminableComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ExtraExaminableComponent, InventoryRelayedEvent<ExaminedEvent>>(OnExaminedWorn);
    }

    private void OnExamined(Entity<ExtraExaminableComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExaminedText is not { } examinedText)
            return;

        args.PushMarkup(Loc.GetString(examinedText));
    }

    private void OnExaminedWorn(Entity<ExtraExaminableComponent> ent, ref InventoryRelayedEvent<ExaminedEvent> args)
    {
        if (!_inventory.TryGetContainingSlot(ent.Owner, out var slot) || (slot.SlotFlags & ent.Comp.AllowedSlots) == SlotFlags.NONE)
            return;

        if (!_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
            return;

        if (ent.Comp.WornText is not { } wornText)
            return;

        var text = Loc.GetString(wornText, ("wearer", container.Owner));

        args.Args.PushMarkup(text);
    }
}
