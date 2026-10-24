using Content.Shared._ST.Interaction;
using Content.Shared.Clothing.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Strip.Components;
using Robust.Shared.Containers;

namespace Content.Shared._Moffstation.StowDelay;

/// <summary>
/// Delays moving an item between your hands and storage or your inventory slots, scaled by the item's size.
/// </summary>
public sealed partial class StowDelaySystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedItemSystem _item = default!;
    [Dependency] private SharedStorageSystem _storage = default!;

    [Dependency] private EntityQuery<ClothingComponent> _clothingQuery;
    [Dependency] private EntityQuery<ItemComponent> _itemQuery;
    [Dependency] private EntityQuery<StorageComponent> _storageQuery;
    [Dependency] private EntityQuery<StowDelayMultiplierComponent> _multiplierQuery;

    [SubscribeLocalEvent]
    private void OnExamined(Entity<StowDelayMultiplierComponent> ent, ref ExaminedEvent args)
    {
        var multiplier = ent.Comp.Multiplier;
        if (multiplier <= 0 || MathHelper.CloseTo(multiplier, 1))
            return;

        var faster = multiplier < 1;
        args.PushMarkup(Loc.GetString(ent.Comp.ExamineText,
            ("name", Name(ent)),
            ("faster", faster),
            ("mul", MathF.Round(faster ? 1f / multiplier : multiplier, 1))));
    }

    [SubscribeLocalEvent]
    private void OnStorageDoAfter(Entity<StorageComponent> ent, ref StowStorageDoAfterEvent args)
    {
        if (args.Handled
            || args.Cancelled
            || args.Used is not { } item
            || !_hands.IsHolding(args.User, item)
            || !_hands.CanDrop(args.User, item))
            return;

        args.Handled = args.Location is { } location
            ? _storage.InsertAt(ent.AsNullable(), item, location, out _, args.User, stackAutomatically: false)
            : _storage.PlayerInsertEntityInWorld(ent.AsNullable(), args.User, item);
    }

    [SubscribeLocalEvent]
    private void OnStorageRemoveDoAfter(Entity<StorageComponent> ent, ref UnstowStorageDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used is not { } item)
            return;

        if (args.TransferStorage is not { } netTransfer || args.Location is not { } location)
        {
            args.Handled = _storage.GrabItem(ent.AsNullable(), args.User, item);
            return;
        }

        if (!ent.Comp.Container.Contains(item)
            || !TryGetEntity(netTransfer, out var transfer)
            || !_hands.TryPickup(args.User, item, animate: false))
            return;

        args.Handled = _storage.InsertAt(transfer.Value, item, location, out _, args.User, stackAutomatically: false);
    }

    [SubscribeLocalEvent]
    private void OnTransferAllDoAfter(Entity<StorageComponent> ent, ref TransferAllStorageDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        _storage.TransferEntities(ent, target, args.User, ent.Comp);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnEquipDoAfter(Entity<ItemComponent> ent, ref StowEquipDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !_hands.IsHolding(args.User, ent))
            return;

        EntityUid? swapped = null;
        if (_inventory.TryUnequip(args.User, args.Slot, out var removed, silent: true, predicted: true))
            swapped = removed;

        args.Handled = _inventory.TryEquip(args.User, ent, args.Slot, predicted: true, triggerHandContact: true);

        if (swapped != null)
            _hands.PickupOrDrop(args.User, swapped.Value);
    }

    [SubscribeLocalEvent]
    private void OnUnequipDoAfter(Entity<ItemComponent> ent, ref UnstowEquipDoAfterEvent args)
    {
        if (args.Handled
            || args.Cancelled
            || !_inventory.TryGetSlotEntity(args.User, args.Slot, out var current)
            || current != ent.Owner
            || !_inventory.TryUnequip(args.User, args.Slot, predicted: true, triggerHandContact: true))
            return;

        _hands.PickupOrDrop(args.User, ent);
        args.Handled = true;
    }

    public bool TryStartStorageDelay(EntityUid storage, EntityUid user, EntityUid item, ItemStorageLocation? location = null)
    {
        if (!_hands.IsHolding(user, item) || !_itemQuery.TryComp(item, out var itemComp))
            return false;

        var delay = GetStorageDelay(storage, itemComp);
        return TryStartDoAfter(user, delay, new StowStorageDoAfterEvent(location), storage, storage, item, breakOnMove: false);
    }

    public bool TryStartStorageTransferDelay(
        Entity<StorageComponent?> storage,
        EntityUid user,
        EntityUid item,
        (EntityUid Storage, ItemStorageLocation Location)? transfer = null)
    {
        if (!Resolve(storage, ref storage.Comp, false)
            || !storage.Comp.Container.Contains(item)
            || !_itemQuery.TryComp(item, out var itemComp)
            || !_hands.CanPickupAnyHand(user, item, item: itemComp))
            return false;

        var delay = GetStorageDelay(storage, itemComp);
        if (transfer is { } t)
            delay += GetStorageDelay(t.Storage, itemComp);

        var ev = new UnstowStorageDoAfterEvent(GetNetEntity(transfer?.Storage), transfer?.Location);
        return TryStartDoAfter(user, delay, ev, storage, storage, item, breakOnMove: false, interactionParticles: true);
    }

    public bool TryStartStorageTransferAllDelay(Entity<StorageComponent?> source, EntityUid target, EntityUid user)
    {
        if (!Resolve(source, ref source.Comp, false))
            return false;

        ItemComponent? largest = null;
        foreach (var contained in source.Comp.Container.ContainedEntities)
        {
            if (_itemQuery.TryComp(contained, out var itemComp)
                && (largest == null || _item.GetSizePrototype(itemComp.Size) > _item.GetSizePrototype(largest.Size)))
                largest = itemComp;
        }

        if (largest == null)
            return false;

        var delay = GetStorageDelay(source, largest) + GetStorageDelay(target, largest);
        return TryStartDoAfter(user, delay, new TransferAllStorageDoAfterEvent(), source, target, source, breakOnMove: false);
    }

    public bool TryStartEquipDelay(EntityUid user, EntityUid target, EntityUid item, string slot)
    {
        if (user != target
            || !_hands.IsHolding(user, item)
            || !_itemQuery.TryComp(item, out var itemComp)
            || !_inventory.TryGetSlot(target, slot, out var slotDefinition))
            return false;

        var delay = GetSlotDelay((item, itemComp), slotDefinition, unequip: false);
        if (_inventory.TryGetSlotEntity(target, slot, out var occupant))
        {
            if (!_itemQuery.TryComp(occupant, out var occupantComp)
                || !_inventory.CanUnequip(user, slot, out _))
                return false;

            delay += GetSlotDelay((occupant.Value, occupantComp), slotDefinition, unequip: true);
        }

        return TryStartDoAfter(user, delay, new StowEquipDoAfterEvent(slot), item, target, item, BreakOnMove(item, slotDefinition), interactionParticles: true);
    }

    public bool TryStartUnequipDelay(EntityUid user, EntityUid target, string slot)
    {
        if (user != target
            || !_inventory.TryGetSlotEntity(target, slot, out var item)
            || !_itemQuery.TryComp(item, out var itemComp)
            || !_inventory.TryGetSlot(target, slot, out var slotDefinition))
            return false;

        var delay = GetSlotDelay((item.Value, itemComp), slotDefinition, unequip: true);
        // Only on the suitstorage should it pop up, since you can put weapons and stuff on it
        var displayParticles = (slotDefinition.SlotFlags & SlotFlags.SUITSTORAGE) != 0;
        return TryStartDoAfter(user, delay, new UnstowEquipDoAfterEvent(slot), item.Value, target, item.Value, BreakOnMove(item.Value, slotDefinition), interactionParticles: displayParticles);
    }

    public bool TryStartPickupDelay(EntityUid user, EntityUid item)
    {
        if (!_container.TryGetContainingContainer(item, out var container))
            return false;

        if (_storageQuery.HasComp(container.Owner))
            return TryStartStorageTransferDelay(container.Owner, user, item);

        return container.Owner == user
            && _inventory.TryGetSlot(user, container.ID, out _)
            && TryStartUnequipDelay(user, user, container.ID);
    }

    private TimeSpan GetStorageDelay(EntityUid storage, ItemComponent item)
    {
        var delay = _item.GetSizePrototype(item.Size).StowDelay;
        if (_multiplierQuery.TryComp(storage, out var multiplier))
            delay *= multiplier.Multiplier;

        return delay;
    }

    private TimeSpan GetSlotDelay(Entity<ItemComponent> item, SlotDefinition slot, bool unequip)
    {
        var delay = _item.GetSizePrototype(item.Comp.Size).StowDelay;
        if (!_clothingQuery.TryComp(item, out var clothing) || (clothing.Slots & slot.SlotFlags) == 0)
            return delay;

        var clothingDelay = unequip ? clothing.UnequipDelay : clothing.EquipDelay;
        return clothingDelay > delay ? clothingDelay : delay;
    }

    private bool BreakOnMove(EntityUid item, SlotDefinition slot)
    {
        if ((slot.SlotFlags & SlotFlags.POCKET) != 0)
            return false;

        return !_clothingQuery.TryComp(item, out var clothing) || !clothing.EquipWhileMoving;
    }

    private bool TryStartDoAfter(
        EntityUid user,
        TimeSpan delay,
        DoAfterEvent ev,
        EntityUid eventTarget,
        EntityUid target,
        EntityUid item,
        bool breakOnMove,
        bool interactionParticles = false)
    {
        if (delay <= TimeSpan.Zero)
            return false;

        var stealthEv = new BeforeStripEvent(TimeSpan.Zero);
        RaiseLocalEvent(user, ref stealthEv);

        var started = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, delay, ev, eventTarget, target, item)
        {
            Hidden = stealthEv.Stealth,
            BreakOnMove = breakOnMove,
            NeedHand = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        });

        if (started && interactionParticles && !stealthEv.Stealth)
            _interaction.DoContactInteraction(user, eventTarget, item, true, interactionParticleType: StellarInteractionParticleType.InHand);

        return true;
    }
}
