using Content.Shared.Administration.Logs;
using Content.Shared.Buckle;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Strip.Components;
using Robust.Shared.Timing;

namespace Content.Shared._Starfall.Offering;

/// <summary>
/// Handles consensual hand-to-hand item transfers.
/// </summary>
public sealed partial class OfferingSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnStripHandInsertStarted(Entity<HandsComponent> offerer, ref StripHandInsertStartedEvent args)
    {
        var offered = EnsureComp<OfferedItemComponent>(args.Item);
        offered.Offerer = offerer;
        offered.Recipient = args.Recipient;
        Dirty(args.Item, offered);
    }

    [SubscribeLocalEvent]
    private void OnStrippableDoAfter(Entity<StrippableComponent> offerer, ref StrippableDoAfterEvent args)
    {
        // The offer only lasts as long as the hand-insert doafter that made it.
        if (!args.InsertOrRemove || args.InventoryOrHand || args.Used is not { } item)
            return;

        if (TryComp<OfferedItemComponent>(item, out var offered) && offered.Offerer == offerer.Owner)
            RemComp(item, offered);
    }

    [SubscribeLocalEvent(before: [typeof(SharedBuckleSystem), typeof(InteractionPopupSystem)])]
    private void OnInteractHand(Entity<StrippableComponent> offerer, ref InteractHandEvent args)
    {
        if (args.Handled ||
            args.User == offerer.Owner ||
            _hands.GetActiveItem(args.User) != null ||
            !TryComp<HandsComponent>(offerer, out var offererHands))
            return;

        foreach (var handName in offererHands.Hands.Keys)
        {
            if (!_hands.TryGetHeldItem((offerer.Owner, offererHands), handName, out var item) ||
                !TryComp<OfferedItemComponent>(item, out var offered) ||
                offered.Offerer != offerer.Owner ||
                offered.Recipient != args.User)
                continue;

            args.Handled = TryAccept(args.User, (offerer.Owner, offererHands), item.Value, handName);
            return;
        }
    }

    [SubscribeLocalEvent]
    private void OnBeforeStripHandRemove(Entity<OfferedItemComponent> item, ref BeforeStripHandRemoveEvent args)
    {
        if (item.Comp.Offerer != args.Holder ||
            item.Comp.Recipient != args.User ||
            !TryComp<HandsComponent>(args.Holder, out var holderHands))
            return;

        args.Handled = TryAccept(args.User, (args.Holder, holderHands), item.Owner, args.HandName);
    }

    [SubscribeLocalEvent]
    private void OnParentChanged(Entity<OfferedItemComponent> item, ref EntParentChangedMessage args)
    {
        if (_timing.ApplyingState)
            return;

        // An offer is only valid while the original offerer continuously holds the item.
        if (!TryComp<HandsComponent>(item.Comp.Offerer, out var hands) ||
            !_hands.IsHolding((item.Comp.Offerer, hands), item.Owner))
            RemCompDeferred<OfferedItemComponent>(item);
    }

    private bool TryAccept(EntityUid recipient, Entity<HandsComponent> offerer, EntityUid item, string handName)
    {
        if (!_hands.CanDropHeld(offerer, handName, checkActionBlocker: false) ||
            !_hands.CanPickupAnyHand(recipient, item))
        {
            PopupFailure(recipient);
            return false;
        }

        // Remove the offer first so a duplicate event this tick can't accept it twice.
        RemComp<OfferedItemComponent>(item);
        if (!_hands.TryDrop(offerer.AsNullable(), item, checkActionBlocker: false))
        {
            PopupFailure(recipient);
            return false;
        }

        if (!_hands.TryPickupAnyHand(recipient, item))
        {
            _hands.TryPickup(offerer, item, handName, checkActionBlocker: false, handsComp: offerer.Comp);
            PopupFailure(recipient);
            return false;
        }

        _adminLogger.Add(LogType.Stripping, LogImpact.Medium, $"{ToPrettyString(recipient):actor} accepted the item {ToPrettyString(item):item} offered by {ToPrettyString(offerer):target}");
        _popup.PopupEntity(Loc.GetString("offering-system-accepted-self", ("item", item)), recipient, recipient);
        return true;
    }

    private void PopupFailure(EntityUid recipient)
    {
        _popup.PopupEntity(Loc.GetString("offering-system-cannot-accept"), recipient, recipient);
    }
}
