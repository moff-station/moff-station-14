using System.Linq;
using Content.Shared._Moffstation.Cards.Components;
using Content.Shared._Moffstation.Cards.Events;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Cards.Systems;

// This part handles PlayingCardComponent.
public sealed partial class PlayingCardsSystem
{
    /// The ID of the entity prototype which is used to construct cards dynamically.
    private static readonly EntProtoId<PlayingCardComponent> BaseCardEntId = "PlayingCardDynamic";

    private void InitCard()
    {
        SubscribeLocalEvent<PlayingCardComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PlayingCardComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<PlayingCardComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PlayingCardComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<PlayingCardComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PlayingCardComponent, ActivateInWorldEvent>(OnActiveInWorld);
        SubscribeLocalEvent<PlayingCardComponent, PlayingCardFlippedEvent>(OnFlipped);
    }


    public void Flip(Entity<PlayingCardComponent> card, bool? faceDown)
    {
        if (!SetFacingOrFlip(card, faceDown))
            return;

        var parentUid = Transform(card).ParentUid;
        if (TryComp<PlayingCardDeckComponent>(parentUid, out var deck))
        {
            deck.DirtyVisuals = true;
        }

        if (TryComp<PlayingCardHandComponent>(parentUid, out var hand))
        {
            hand.DirtyVisuals = true;
        }
    }


    private void OnStartup(Entity<PlayingCardComponent> entity, ref ComponentStartup args)
    {
        var ev = new PlayingCardFlippedEvent();
        RaiseLocalEvent(entity, ref ev);
    }

    private void OnExamined(Entity<PlayingCardComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || entity.Comp.FaceDown)
            return;

        args.PushMarkup(Loc.GetString("card-examined", ("target", Loc.GetString(entity.Comp.Name))));
    }

    private void OnGetVerbs(Entity<PlayingCardComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => Flip(entity, faceDown: null),
            Text = Loc.GetString("cards-verb-flip"),
            Icon = entity.Comp.FlipIcon,
            Priority = 1,
        });
    }

    private void OnUse(Entity<PlayingCardComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        Flip(entity, faceDown: null);
        args.Handled = true;
    }

    private void OnInteractUsing(Entity<PlayingCardComponent> entity, ref InteractUsingEvent args)
    {
        if (TryComp<PlayingCardDeckComponent>(args.Used, out var usedDeck))
        {
            Add<PlayingCardDeckComponent>((args.Used, usedDeck), [entity], Transform(entity).Coordinates, args.User);
            args.Handled = true;
            return;
        }

        if (TryComp<PlayingCardHandComponent>(args.Used, out var usedHand))
        {
            Add<PlayingCardHandComponent>((args.Used, usedHand), [entity], Transform(entity).Coordinates, args.User);
            args.Handled = true;
            return;
        }

        if (TryComp<PlayingCardComponent>(args.Used, out var usedCard) &&
            CreateHand([(args.Used, usedCard), entity], args.User) is { } hand)
        {
            _hands.PickupOrDrop(args.User, hand);
            args.Handled = true;
            return;
        }
    }

    private void OnActiveInWorld(Entity<PlayingCardComponent> entity, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        Flip(entity, faceDown: null);

        args.Handled = true;
    }

    private void OnFlipped(Entity<PlayingCardComponent> entity, ref PlayingCardFlippedEvent args)
    {
        _metadata.SetEntityName(entity, entity.Comp.FaceDown ? entity.Comp.ReverseName : entity.Comp.Name);
        _metadata.SetEntityDescription(entity,
            entity.Comp.FaceDown ? (entity.Comp.ReverseDescription ?? "") : entity.Comp.Description);
        _appearance.SetData(entity, PlayingCardVisuals.IsFaceDown, entity.Comp.FaceDown);
        Dirty(entity);
    }


    /// Sets the card's facing to <paramref name="faceDown"/>, or flips it if <paramref name="faceDown"/> is null.
    /// Returns whether or not the card's facing was changed.
    private bool SetFacingOrFlip(Entity<PlayingCardComponent> card, bool? faceDown)
    {
        var didFlip = SetOrInvert(ref card.Comp.FaceDown, faceDown);
        if (didFlip)
        {
            var ev = new PlayingCardFlippedEvent();
            RaiseLocalEvent(card, ref ev);
        }

        return didFlip;
    }

    /// Sets <paramref name="value"/> to <paramref name="setToValue"/>, or inverts it if <paramref name="setToValue"/>
    /// is null. Returns whether or not the value changed.
    private static bool SetOrInvert(ref bool value, bool? setToValue)
    {
        var newValue = setToValue ?? !value;
        var valueChanged = newValue != value;

        value = newValue;
        return valueChanged;
    }

    /// "Instantiates" <paramref name="cardData"/> as the returned entity using the information passed. Returns null if
    /// the deck prototype cannot be resolved, as we can't make dynamic cards without that.
    /// Note that the spawned card <b>is predicted on the client</b>.
    private Entity<PlayingCardComponent>? SpawnPredictedDynamicCard(
        PlayingCardDeckPrototypeElementData cardData,
        Entity<PlayingCardDeckComponent> deck,
        EntityCoordinates coords
    )
    {
        // If we can't get the deck's deck proto, we won't be able to make a complete card.
        if (!_proto.Resolve(deck.Comp.Prototype, out var deckProto))
            return null;

        var spawned = PredictedSpawnAtPosition(BaseCardEntId, coords);
        var cardComp = Comp<PlayingCardComponent>(spawned);
        var (obverseSprite, reverseSprite) = ToLayers(cardData, deckProto);
        cardComp.FaceDown = cardData.FaceDown;
        cardComp.ObverseSprite = obverseSprite;
        cardComp.Name = deckProto.CardName(cardData.Id);
        cardComp.Description = deckProto.CardDescription(cardData.Id);
        cardComp.ReverseSprite = reverseSprite;
        cardComp.ReverseName = deckProto.ReverseName();
        cardComp.ReverseDescription = deckProto.ReverseDescription();

        Dirty(spawned, cardComp);

        var ev = new PlayingCardFlippedEvent();
        RaiseLocalEvent(spawned, ref ev);

        return (spawned, cardComp);
    }

    /// Calculates the obverse and reverse sprite layers for <paramref name="cardData"/>, using the common/default info
    /// from <paramref name="deckProto"/>.
    private static (PrototypeLayerData[] obverse, PrototypeLayerData[] reverse) ToLayers(
        PlayingCardDeckPrototypeElementData cardData,
        PlayingCardDeckPrototype deckProto
    )
    {
        // Obverse layers are the deck's common obverse layers, plus the card's own obverse layers.
        var obverse = deckProto.CommonObverseSprite.Concat(
                // Use the explicitly defined obverse sprite, or make a layer whose state is the card's ID.
                cardData.ObverseSprite ??
                [new PrototypeLayerData { State = cardData.Id }]
            )
            // Default to the deck's RSI if the layers don't have one already.
            // This allows yamlers to specify only the state.
            .WithUnlessAlreadySpecified(rsiPath: deckProto.RsiPath.ToString());

        // Default to the deck's RSI if the layers don't have one already. This allows yamlers to specify only the state
        // on the reverse sprite layers.
        var reverse =
            deckProto.CommonReverseSprite.WithUnlessAlreadySpecified(rsiPath: deckProto.RsiPath.ToString());

        return (obverse, reverse);
    }
}
