using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Robust.Shared.Containers;

namespace Content.Shared._Moffstation.Clothing;

public sealed class ExtraExaminableSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private const string OwnerString = "wearer";
    private const string ItemString = "item";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExtraExaminableComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ExtraExaminableComponent, InventoryRelayedEvent<ExaminedEvent>>(OnExaminedWorn);
        SubscribeLocalEvent<ExtraExaminableComponent, HeldRelayedEvent<ExaminedEvent>>(OnExaminedHeld);
    }

    private void OnExamined(Entity<ExtraExaminableComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExaminedText is not { } examinedText)
            return;

        var text = GetString(examinedText, null, ent);

        args.PushMarkup(text);
    }

    private void OnExaminedHeld(Entity<ExtraExaminableComponent> ent, ref HeldRelayedEvent<ExaminedEvent> args)
    {
        if (ent.Comp.HeldText is not { } heldText)
            return;

        var text = GetString(heldText, args.Args.Examined, ent);

        args.Args.PushMarkup(text);
    }

    private void OnExaminedWorn(Entity<ExtraExaminableComponent> ent, ref InventoryRelayedEvent<ExaminedEvent> args)
    {
        if (!_inventory.TryGetContainingSlot(ent.Owner, out var slot) || (slot.SlotFlags & ent.Comp.AllowedSlots) == SlotFlags.NONE)
            return;

        if (!_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
            return;

        if (ent.Comp.WornText is not { } wornText)
            return;

        var text = GetString(wornText, container.Owner, ent);

        args.Args.PushMarkup(text);
    }

    // Formats the string based on the item and the person holding it. Useful for us so long as we use the same format
    private string GetString(LocId locId, EntityUid? owner, Entity<ExtraExaminableComponent> item)
    {
        var itemArg = (ItemString, item.Owner);

        return owner is { } ownerUid
            ? Loc.GetString(locId, itemArg, (OwnerString, Identity.Entity(ownerUid, EntityManager)))
            : Loc.GetString(locId, itemArg);
    }
}
