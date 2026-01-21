using System.Linq;
using Content.Shared._Moffstation.Cards.Components;
using Content.Shared._Moffstation.Cards.Events;
using Content.Shared._Moffstation.Cards.Prototypes;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Cards.Systems;

// This part handles PlayingCardComponent.
public abstract partial class SharedPlayingCardsSystem
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

        args.PushMarkup(Loc.GetString("card-examined", ("target", entity.Comp.Name)));
    }

    private void OnGetVerbs(Entity<PlayingCardComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => Flip(entity, faceDown: null),
            Text = Loc.GetString(entity.Comp.FlipText),
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

    /// "Instantiates" <paramref name="data"/> as the returned entity. Returns null if resolving prototypes fails.
    /// Note that the spawned card <b>is predicted on the client</b>.
    private Entity<PlayingCardComponent>? SpawnPredictedDynamicCard(
        PlayingCardInDeckUnspawnedData data,
        EntityCoordinates coords
    )
    {
        var spawned = PredictedSpawnAtPosition(BaseCardEntId, coords);
        var cardComp = Comp<PlayingCardComponent>(spawned);
        TryApplyCardData(ref cardComp, data);
        Dirty(spawned, cardComp);

        var ev = new PlayingCardFlippedEvent();
        RaiseLocalEvent(spawned, ref ev);

        ForceAppearanceUpdate((spawned, cardComp));

        return (spawned, cardComp);
    }

    /// Returns a new <see cref="PlayingCardComponent"/> with the given <paramref name="data"/>. Returns null if
    /// prototype resolution fails.
    private PlayingCardComponent? ToComponent(PlayingCardInDeckUnspawnedData data)
    {
        var comp = _compFact.GetComponent<PlayingCardComponent>();
        if (!TryApplyCardData(ref comp, data))
            return null;

        return WithFacing(comp, data.Card.FaceDown);
    }

    /// Applies the given <paramref name="data"/> to the given <paramref name="comp"/>. Returns whether or not the
    /// component was modified, ie. returns false if prototype resolution failed.
    private bool TryApplyCardData(ref PlayingCardComponent comp, PlayingCardInDeckUnspawnedData data)
    {
        PlayingCardSuitPrototype? suit = null;
        if (!_proto.Resolve(data.Deck, out var deck) ||
            data.Suit is { } suitId &&
            !_proto.Resolve(suitId, out suit))
            return false;

        comp.ObverseLayers = AssembleObverseSpriteLayers(data.Card, deck, suit);
        comp.ReverseLayers = deck.CommonReverseLayers.WithUnlessAlreadySpecified(rsiPath: deck.RsiPath.ToString());
        comp.FaceDown = data.Card.FaceDown;

        (string, object)[] locArgs = suit is null
            ? [("card", Loc.GetString(deck.CardValueLoc, ("card", data.Card.Id.ToLowerInvariant())))]
            :
            [
                ("suit", Loc.GetString(deck.SuitLoc, ("suit", suit.ID.ToLowerInvariant()))),
                ("card", Loc.GetString(deck.CardValueLoc, ("card", data.Card.Id.ToLowerInvariant()))),
            ];

        comp.Name = Loc.GetString(data.Card.NameLoc ?? deck.CardNameLoc, locArgs);
        comp.Description = Loc.GetString(deck.CardDescLoc, locArgs);
        comp.ReverseName = Loc.GetString(deck.CardReverseNameLoc, locArgs);
        comp.ReverseDescription = Loc.GetString(deck.CardReverseDescLoc, locArgs);

        return true;
    }

    private static PrototypeLayerData[] AssembleObverseSpriteLayers(
        PlayingCardDeckPrototypeElementCard card,
        PlayingCardDeckPrototype deck,
        PlayingCardSuitPrototype? suit
    )
    {
        // If neither the card nor the suit specify if the deck's common layers should be used, default to true.
        var layersFromDeck = card.UseDeckLayers ?? suit?.UseDeckLayers ?? true ? deck.CommonObverseLayers : [];
        var layersFromSuit = card.UseSuitLayers ? suit?.CommonObverseLayers ?? [] : [];

        PrototypeLayerData[] layersFromCard;
        if (card.ObverseLayers is { } layers)
        {
            // If there're layers specifically added to this card, replace `{suit}` in the state IDs with the actual suit ID.
            layersFromCard = layers.Select(l =>
                {
                    var layerCopy = l.With();
                    layerCopy.State = layerCopy.State?.Replace("{suit}", suit?.ID.ToLowerInvariant() ?? "");
                    return layerCopy;
                })
                .ToArray();
        }
        else
        {
            var stateFromCard = card.Id.Replace("{suit}", suit?.ID.ToLowerInvariant() ?? "");

            // No layers specified on the card, so assemble the default state ID from the deck, suit and card IDs.
            var stateFromSuit = suit is { DefaultObverseLayerState: { } suitState }
                ? suitState.Replace("{card}", stateFromCard)
                : card.Id;

            var stateFromDeck = deck.DefaultObverseLayerState is { } deckState
                ? deckState.Replace("{card}", stateFromSuit)
                : stateFromSuit;
            layersFromCard = [new PrototypeLayerData { State = stateFromDeck }];
        }

        // Assemble all of the layers together and default the deck's RSI if any layers are missing it.
        return layersFromDeck.Concat(layersFromSuit)
            .Concat(layersFromCard)
            .WithUnlessAlreadySpecified(rsiPath: deck.RsiPath.ToString());
    }

    protected abstract void ForceAppearanceUpdate(Entity<PlayingCardComponent> card);
}
