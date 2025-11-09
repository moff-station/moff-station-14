using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Verbs;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Strip.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Shared._Moffstation.Cards;

public abstract class SharedCardSystem : EntitySystem
{
    [Dependency] private readonly ILogManager _logManager = default!;
    protected ISawmill Sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CardComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CardComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
        SubscribeLocalEvent<CardComponent, ItemToggledEvent>(OnItemToggled);
        SubscribeLocalEvent<CardComponent, ExaminedEvent>(OnExamined);

        // When a CardVisibilityChangedEvent is raised on a card, forward it to the owning strip target.
        SubscribeLocalEvent<CardComponent, CardVisibilityChangedEvent>(OnCardVisibilityChanged);
    }

    private void OnExamined(EntityUid uid, CardComponent card, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!card.IsFaceUp)
        {
            args.PushText("Its face is turned away; you can’t tell which card it is.");
        }
        else
        {
            args.PushText($"A standard playing card reading a {card.Value} of {card.Suit}.");
        }
    }

    private void OnAfterInteract(EntityUid uid, CardComponent card, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target != uid)
            return;

        if (!CanFlip(uid, card, args.User))
            return;

        FlipCard(uid, card, args.User);
        args.Handled = true;
    }

    private void OnGetInteractionVerbs(EntityUid uid, CardComponent card, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!CanFlip(uid, card, args.User))
            return;

        var verbText = card.IsFaceUp ? "Flip face-down" : "Flip face-up";

        var verb = new InteractionVerb
        {
            Text = verbText,
            Act = () =>
            {
                FlipCard(uid, card, args.User);
            },
        };

        args.Verbs.Add(verb);
    }

    private void OnItemToggled(EntityUid uid, CardComponent card, ref ItemToggledEvent args)
    {
        var user = args.User ?? uid;
        if (!CanFlip(uid, card, user))
            return;

        var newFaceUp = args.Activated; // On = face-up, Off = face-down.

        if (card.IsFaceUp == newFaceUp)
            return;

        card.IsFaceUp = newFaceUp;
        Dirty(uid, card);

        // Notify listeners (card + owner) that visible state changed.
        var ev = new CardVisibilityChangedEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    protected virtual bool CanFlip(EntityUid uid, CardComponent card, EntityUid user)
        => true;

    protected void FlipCard(EntityUid uid, CardComponent card, EntityUid user)
    {
        var old = card.IsFaceUp;
        card.IsFaceUp = !card.IsFaceUp;
        Dirty(uid, card);

        if (old == card.IsFaceUp)
            return;

        var ev = new CardVisibilityChangedEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    private void OnCardVisibilityChanged(EntityUid uid, CardComponent comp, ref CardVisibilityChangedEvent args)
    {
        // Forward CardVisibilityChangedEvent from the card to its owning strip target (if any).
        var entMan = EntityManager;
        var current = uid;
        EntityUid? owner = null;

        // Walk up transform parents to find an entity with StrippableComponent.
        while (entMan.TryGetComponent(current, out TransformComponent? xform) &&
               xform.ParentUid.IsValid() &&
               xform.ParentUid != current)
        {
            current = xform.ParentUid;

            if (entMan.HasComponent<StrippableComponent>(current))
            {
                owner = current;
                break;
            }
        }

        if (owner == null)
        {
            return;
        }

        var ownerEv = new CardVisibilityChangedEvent();
        RaiseLocalEvent(owner.Value, ref ownerEv);
    }
}
