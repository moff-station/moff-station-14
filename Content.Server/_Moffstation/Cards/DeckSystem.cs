using Content.Shared._Moffstation.Cards;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server._Moffstation.Cards;

public sealed class DeckSystem : SharedDeckSystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeckComponent, ComponentInit>(OnDeckInit);
        SubscribeLocalEvent<DeckComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DeckComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
    }

    private void OnDeckInit(EntityUid uid, DeckComponent deck, ComponentInit args)
    {
        // Ensure spawned decks obey "top = last" before any flips/draws.
        NormalizeTopAsLast(uid, deck);
    }

    private void OnAfterInteract(EntityUid uid, DeckComponent deck, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target != uid)
            return;

        if (!EntityManager.TryGetComponent(args.User, out HandsComponent? hands))
            return;

        var user = args.User;
        var held = _hands.GetActiveItem(user);

        // If holding a card, place it on top.
        if (held is { } heldUid &&
            EntityManager.TryGetComponent(heldUid, out CardComponent? heldCard))
        {
            if (TryPutOnTop(uid, deck, heldUid, heldCard))
            {
                _hands.TryDrop(user, heldUid, checkActionBlocker: false);
                args.Handled = true;
            }

            return;
        }

        // Otherwise, draw from top (last).
        if (!TryDrawTop(uid, deck, out var drawn, out _))
            return;

        _hands.TryPickupAnyHand(user, drawn);
        args.Handled = true;
    }

    private void OnGetVerbs(EntityUid uid, DeckComponent deck, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        // Flip verb.
        var flip = new InteractionVerb
        {
            Text = "Flip deck",
            Act = () => FlipDeck(uid, deck)
        };
        args.Verbs.Add(flip);

        // Insert-on-top.
        if (EntityManager.TryGetComponent(user, out HandsComponent? hands))
        {
            var held = _hands.GetActiveItem(user);
            if (held is { } heldUid &&
                EntityManager.TryGetComponent(heldUid, out CardComponent? heldCard))
            {
                var insert = new InteractionVerb
                {
                    Text = "Place card on top",
                    Act = () =>
                    {
                        if (!TryPutOnTop(uid, deck, heldUid, heldCard))
                            return;

                        _hands.TryDrop(user, heldUid, checkActionBlocker: false);
                    }
                };
                args.Verbs.Add(insert);
            }
        }

        // Draw-from-top verb.
        var draw = new InteractionVerb
        {
            Text = "Draw from top",
            Act = () =>
            {
                if (!TryDrawTop(uid, deck, out var drawn, out _))
                    return;

                if (EntityManager.TryGetComponent(user, out HandsComponent? _))
                    _hands.TryPickupAnyHand(user, drawn);
            }
        };
        args.Verbs.Add(draw);
    }
}
