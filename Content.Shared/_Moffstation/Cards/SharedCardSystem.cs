using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Verbs;
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

        // Handle E/toggle via ItemToggleSystem.
        SubscribeLocalEvent<CardComponent, ItemToggledEvent>(OnItemToggled);
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

    // Map ItemToggle's activated state directly to face-up/face-down.
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
    }

    protected virtual bool CanFlip(EntityUid uid, CardComponent card, EntityUid user)
        => true;

    protected void FlipCard(EntityUid uid, CardComponent card, EntityUid user)
    {
        var old = card.IsFaceUp;
        card.IsFaceUp = !card.IsFaceUp;
        Dirty(uid, card);
    }
}
