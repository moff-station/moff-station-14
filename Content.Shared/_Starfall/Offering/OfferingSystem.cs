using Content.Shared.Buckle;
using Content.Shared.DoAfter;
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
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnStripHandInsertStarted(Entity<HandsComponent> offerer, ref StripHandInsertStartedEvent args)
    {
        var offered = EnsureComp<OfferedItemComponent>(args.Item);
        offered.Offerer = offerer;
        offered.Recipient = args.Recipient;
        offered.DoAfterIndex = args.DoAfterIndex;
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

            args.Handled = TryAccept(args.User, (item.Value, offered));
            return;
        }
    }

    [SubscribeLocalEvent]
    private void OnBeforeStripHandRemove(Entity<OfferedItemComponent> item, ref BeforeStripHandRemoveEvent args)
    {
        if (item.Comp.Offerer != args.Holder || item.Comp.Recipient != args.User)
            return;

        args.Handled = TryAccept(args.User, item);
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

    private bool TryAccept(EntityUid recipient, Entity<OfferedItemComponent> item)
    {
        if (!_doAfter.TryFastForward(item.Comp.Offerer, item.Comp.DoAfterIndex) ||
            !_hands.IsHolding(recipient, item))
        {
            _popup.PopupEntity(Loc.GetString("offering-system-cannot-accept"), recipient, recipient);
            return false;
        }

        _popup.PopupEntity(Loc.GetString("offering-system-accepted-self", ("item", item)), recipient, recipient);
        return true;
    }
}
