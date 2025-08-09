using System.Linq;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.BladeServer;

/// <summary>
/// This system handles behavior for <see cref="BladeServerRackComponent"/>s and <see cref="BladeServerComponent"/>s.
/// </summary>
public abstract partial class SharedBladeServerSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    /// <summary>
    /// This whitelist allows only entities with <see cref="BladeServerComponent"/> to be inserted into the rack.
    /// </summary>
    private EntityWhitelist _slotWhitelist = new();

    public override void Initialize()
    {
        _slotWhitelist = new EntityWhitelist
        {
            Components = [_componentFactory.GetComponentName<BladeServerComponent>()],
        };

        SubscribeLocalEvent<BladeServerRackComponent, AfterAutoHandleStateEvent>(AfterAutoHandleState);
        SubscribeLocalEvent<BladeServerRackComponent, ComponentInit>(OnComponentInit);

        SubscribeLocalEvent<BladeServerRackComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<BladeServerRackComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<BladeServerRackComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<BladeServerRackComponent, ComponentRemove>(OnComponentRemove);

        Subs.BuiEvents<BladeServerRackComponent>(
            BladeServerRackUiKey.Key,
            subs =>
            {
                subs.Event<BladeServerRackEjectPressedMessage>(OnEjectPressed);
                subs.Event<BladeServerRackInsertPressedMessage>(OnInsertPressed);
                subs.Event<BladeServerRackPowerPressedMessage>(OnPowerPressed);
                subs.Event<BladeServerRackUseMessage>(OnUseBladeServerEntityInUi);
                subs.Event<BladeServerRackActivateInWorldMessage>(OnActivateInWorld);
            }
        );

        SubscribeLocalEvent<BladeServerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BladeServerComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUpAttempt);
    }

    private void OnComponentInit(Entity<BladeServerRackComponent> entity, ref ComponentInit args)
    {
        // Fill slots in the rack based on the component's `StartingContents`
        InitializeSlots(
            entity,
            idx => entity.Comp.StartingContents.TryGetValue(idx, out var proto)
                ? SpawnNextToOrDrop(proto, entity)
                : null
        );
    }

    private void AfterAutoHandleState(Entity<BladeServerRackComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        if (entity.Comp.NumSlots == entity.Comp.BladeSlots.Count)
            return;

        // Re-initialize the slots if the number of slots has changed, but using the current contents rather than
        // starting contents.
        InitializeSlots(
            entity,
            idx => entity.Comp.BladeSlots.TryGetValue(idx, out var slot) ? slot.Item : null
        );

        UpdateUiAndVisuals(entity);
    }

    private void OnEntInserted(
        Entity<BladeServerRackComponent> entity,
        ref EntInsertedIntoContainerMessage args)
    {
        if (GetContainingSlotOrNull(entity, args.Entity) is { } slot)
        {
            _itemSlots.SetLock(entity, slot.Slot, true);
        }

        UpdateUiAndVisuals(entity);
    }

    private void OnEntRemoved(Entity<BladeServerRackComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        UpdateUiAndVisuals(entity);
    }

    private void OnPowerChanged(Entity<BladeServerRackComponent> entity, ref PowerChangedEvent args)
    {
        UpdateVisuals(entity);
    }

    private void OnComponentRemove(Entity<BladeServerRackComponent> entity, ref ComponentRemove args)
    {
        ClearSlots(entity);
    }

    private void OnEjectPressed(Entity<BladeServerRackComponent> entity, ref BladeServerRackEjectPressedMessage args)
    {
        if (GetSlotOrNull(entity.AsNullable(), args.Index) is not { } slot ||
            slot.Item == null)
            return;

        _itemSlots.SetLock(entity, slot.Slot, false);
        _itemSlots.TryEjectToHands(entity, slot.Slot, args.Actor);

        // If the item somehow wasn't removed, re-lock the slot.
        if (slot.Item is not null)
        {
            _itemSlots.SetLock(entity, slot.Slot, true);
        }
    }

    private void OnInsertPressed(Entity<BladeServerRackComponent> entity, ref BladeServerRackInsertPressedMessage args)
    {
        if (GetSlotOrNull(entity.AsNullable(), args.Index) is not { Item: null } slot)
            return;

        // Try to insert what the user's holding into the slot.
        _itemSlots.TryInsertFromHand(entity, slot.Slot, args.Actor);
    }

    private void OnPowerPressed(Entity<BladeServerRackComponent> entity, ref BladeServerRackPowerPressedMessage args)
    {
        if (GetSlotOrNull(entity.AsNullable(), args.Index) is not { } slot)
            return;

        SetSlotPower(entity, slot, args.Powered);
    }

    /// <summary>
    /// Handle "use" interactions which are created by the UI. If the slot is empty, this tries to insert whatever the
    /// user is holding into the slot. If the slot is not empty, this just relays the usage to
    /// <see cref="_interaction">the interaction system</see> after checking that the rack is accessible.
    /// </summary>
    private void OnUseBladeServerEntityInUi(Entity<BladeServerRackComponent> entity, ref BladeServerRackUseMessage args)
    {
        if (!_interaction.InRangeAndAccessible(args.Actor, entity.Owner) ||
            GetSlotOrNull(entity.AsNullable(), args.Index) is not { } slot)
            return;

        if (slot.Item is { } slotEntity)
        {
            // Relay the usage to the entity in the slot.
            if (!TryComp(entity, out TransformComponent? xform))
                return;

            _interaction.UserInteraction(
                args.Actor,
                xform.Coordinates,
                slotEntity,
                // It's specifically in a container, so skip access checks.
                // We checked that the server rack is accessible above.
                checkAccess: false
            );
        }
        else
        {
            // The slot is empty, so try to insert what the user's holding into it.
            _itemSlots.TryInsertFromHand(entity, slot.Slot, args.Actor);
        }
    }


    /// <summary>
    /// Handle "activate" interactions which are created by the UI. This just relays the usage to
    /// <see cref="_interaction">the interaction system</see> after checking that the rack is accessible and that there
    /// is a blade server to interact with.
    /// </summary>
    private void OnActivateInWorld(
        Entity<BladeServerRackComponent> entity,
        ref BladeServerRackActivateInWorldMessage args
    )
    {
        if (!_interaction.InRangeAndAccessible(args.Actor, entity.Owner) ||
            GetSlotOrNull(entity.AsNullable(), args.Index)?.Item is not { } bladeServer)
            return;

        if (args.Alternate)
        {
            _interaction.AltInteract(args.Actor, bladeServer);
        }
        else
        {
            _interaction.InteractionActivate(args.Actor, bladeServer);
        }
    }

    /// <summary>
    /// Sets the appearance data for new blade servers to make their stripe colors work.
    /// </summary>
    private void OnComponentInit(Entity<BladeServerComponent> entity, ref ComponentInit args)
    {
        if (entity.Comp.StripeColor is not { } stripeColor)
            return;

        _appearance.SetData(entity, BladeServerVisuals.StripeColor, stripeColor);
    }

    /// <summary>
    /// This prevents blade servers from being picked up while inside a rack.
    /// </summary>
    private void OnGettingPickedUpAttempt(
        Entity<BladeServerComponent> entity,
        ref GettingPickedUpAttemptEvent args
    )
    {
        if (!_container.IsEntityInContainer(entity) ||
            !TryComp(entity, out TransformComponent? xform) ||
            !TryComp<BladeServerRackComponent>(xform.ParentUid, out var parentRack) ||
            GetContainingSlotOrNull((xform.ParentUid, parentRack), entity) is not { } containingSlot)
            return;

        if (containingSlot.Slot.Locked)
        {
            // Apparently even if the containing slot is locked, if you can directly left click on the entity, it'll be
            // picked up "through" the slot.
            args.Cancel();
        }
    }

    /// <summary>
    /// Different from just "is powered" because this DOES NOT care about the rack itself being powered.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="slotIndex"></param>
    /// <returns></returns>
    public bool? IsSlotPowerEnabled(Entity<BladeServerRackComponent?> entity, int slotIndex)
    {
        return GetSlotOrNull(entity, slotIndex)?.IsPowerEnabled;
    }

    /// <summary>
    /// Tries to get the slot at <paramref name="slotIndex"/>. Returns null if the index is out of bounds.
    /// </summary>
    protected BladeSlot? GetSlotOrNull(Entity<BladeServerRackComponent?> entity, int slotIndex)
    {
        if (!Resolve(entity, ref entity.Comp))
            return null;

        if (slotIndex < 0 ||
            !entity.Comp.BladeSlots.TryGetValue(slotIndex, out var slot))
            return null;

        return slot;
    }

    [MustCallBase]
    protected virtual void SetSlotPower(Entity<BladeServerRackComponent> entity, BladeSlot slot, bool powered)
    {
        slot.IsPowerEnabled = powered;
        Dirty(entity);
        UpdateUiAndVisuals(entity);
    }

    /// <summary>
    /// Tries to find the <see cref="BladeSlot"/> in <paramref name="entity"/> which contains
    /// <paramref name="bladeServerEntity"/>. Returns null if no such slot exists.
    /// </summary>
    private static BladeSlot? GetContainingSlotOrNull(
        Entity<BladeServerRackComponent> entity,
        EntityUid bladeServerEntity
    )
    {
        foreach (var slot in entity.Comp.BladeSlots)
        {
            if (slot.Item == bladeServerEntity)
                return slot;
        }

        return null;
    }

    /// <summary>
    /// <see cref="ClearSlots">Clears</see> and re-initializes <see cref="BladeServerRackComponent.BladeSlots"/> for
    /// <paramref name="entity"/>.
    /// </summary>
    /// <param name="getEntityToInsertForIndex">
    /// A function which, given a slot index, returns the entity to put into that slot. If the slots are being
    /// initialized for the first time, this could perhaps look up protoIds and spawn entities; if the slots are being
    /// re-constructed for some reason, this could perhaps return entiteis which previously existed in this rack. A
    /// return value of null indicates the slot should be left empty.
    /// </param>
    private void InitializeSlots(
        Entity<BladeServerRackComponent> entity,
        Func<int, EntityUid?> getEntityToInsertForIndex
    )
    {
        ClearSlots(entity);

        if (entity.Comp.NumSlots <= 0)
            return;

        foreach (var idx in Enumerable.Range(0, entity.Comp.NumSlots))
        {
            var slot = new ItemSlot { Whitelist = _slotWhitelist };

            _itemSlots.AddItemSlot(entity, entity.Comp.BladeSlotName(idx), slot);

            var inserted = getEntityToInsertForIndex(idx) is { } entityToInsert &&
                           _itemSlots.TryInsert(entity, slot, entityToInsert, user: null);

            _itemSlots.SetLock(entity, slot, inserted);

            entity.Comp.BladeSlots.Add(new BladeSlot(slot));
        }
    }

    /// <summary>
    /// Ejects all slot contents, removes all slots from this entity, and clears
    /// <see cref="BladeServerRackComponent.BladeSlots"/>. This leaves the component in a kind of inoperable state, so
    /// this should only ever be called before re-initializing the slots or as part of destroying the component.
    /// </summary>
    /// <param name="entity"></param>
    private void ClearSlots(Entity<BladeServerRackComponent, ItemSlotsComponent?> entity)
    {
        if (!TryComp(entity, out entity.Comp2))
            return;

        foreach (var slot in entity.Comp1.BladeSlots)
        {
            _itemSlots.SetLock(entity, slot.Slot, false);
            _itemSlots.TryEject(entity, slot.Slot, user: null, out _);
            _itemSlots.RemoveItemSlot(entity, slot.Slot, entity);
        }

        entity.Comp1.BladeSlots.Clear();
    }

    private void UpdateUiAndVisuals(Entity<BladeServerRackComponent> entity)
    {
        UpdateUi(entity);
        UpdateVisuals(entity);
    }

    private void UpdateUi(Entity<BladeServerRackComponent> entity)
    {
        _ui.SetUiState(
            entity.Owner,
            BladeServerRackUiKey.Key,
            new BladeServerRackBoundUserInterfaceState(
                entity.Comp.BladeSlots.Select(it =>
                        new BladeServerRackBoundUserInterfaceState.Slot(
                            GetNetEntity(it.Item),
                            it.IsPowerEnabled)
                    )
                    .ToList()
            )
        );
    }

    /// <summary>
    /// Synchs appearance data for <paramref name="entity"/>.
    /// </summary>
    private void UpdateVisuals(Entity<BladeServerRackComponent> entity)
    {
        var rackPowered = _powerReceiver.IsPowered(entity.Owner);

        var data = entity.Comp.BladeSlots.Select((bladeSlot, idx) =>
        {
            if (bladeSlot.Item is not { } bladeServer ||
                !TryComp<BladeServerComponent>(bladeServer, out var blade))
                return null;

            return new BladeServerRackSlotVisualData(
                blade.StripeColor,
                rackPowered && bladeSlot.IsPowerEnabled,
                entity.Comp.BladeSlotSpritePixelOffsetToLayerOffset(idx)
            );
        });

        _appearance.SetData(
            entity.Owner,
            BladeServerRackVisuals.SlotsKey,
            new BladeServerRackSlotVisualData.Group(data)
        );
    }
}
