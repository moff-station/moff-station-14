using System.Linq;
using Content.Shared._Moffstation.Cards.Components;
using Content.Shared._Moffstation.Cards.Events;
using Content.Shared._Moffstation.Cards.Prototypes;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Cards.Systems;

// This part handles PlayingCardDeckComponent.
public abstract partial class SharedPlayingCardsSystem
{
    /// The ID of the entity prototype which is used to construct cards dynamically.
    private static readonly EntProtoId<PlayingCardDeckComponent> CardDeckEntId = "PlayingCardDeckDynamic";

    private void InitDeck()
    {
        SubscribeLocalEvent<PlayingCardDeckComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PlayingCardDeckComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PlayingCardDeckComponent, ExaminedEvent>(OnExaminedDeck);
        SubscribeLocalEvent<PlayingCardDeckComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PlayingCardDeckComponent, InteractHandEvent>(OnInteractHand,
            before: new[] { typeof(SharedItemSystem) });
        SubscribeLocalEvent<PlayingCardDeckComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbsDeck);
        SubscribeLocalEvent<PlayingCardDeckComponent, GetVerbsEvent<UtilityVerb>>(OnGetUtilityVerbsStack);
        SubscribeLocalEvent<PlayingCardDeckComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbsDeck);
        SubscribeLocalEvent<PlayingCardDeckComponent, PlayingCardStackContentsChangedEvent>(DirtyVisuals);
        SubscribeLocalEvent<PlayingCardDeckComponent, ContainedPlayingCardFlippedEvent>(DirtyVisuals);
    }

    private void OnInit(Entity<PlayingCardDeckComponent> entity, ref ComponentInit args)
    {
        // Initialize the contents of the deck from the prototype.
        if (entity.Comp.Prototype is not { } proto ||
            // Don't overwrite existing cards.
            entity.Comp.Cards.Count != 0)
            return;

        // Reverse the cards so that the first in the prototype's list is on the top.
        entity.Comp.Cards = GetCards(proto).Reverse().ToList();
        Dirty(entity);
    }

    private void OnExaminedDeck(Entity<PlayingCardDeckComponent> entity, ref ExaminedEvent args)
    {
        if (entity.Comp.TopCard is { } topCardLike &&
            GetComponent(topCardLike) is { FaceDown: false } topCard)
        {
            args.PushMarkup(Loc.GetString(entity.Comp.TopCardExamineLoc, ("card", topCard.Name)));
        }

        OnExamined(entity, ref args);
    }

    private void OnInteractHand(Entity<PlayingCardDeckComponent> entity, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryDraw(entity, args.User);
    }

    private void OnGetInteractionVerbsDeck(
        Entity<PlayingCardDeckComponent> entity,
        ref GetVerbsEvent<InteractionVerb> args
    )
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        // Draw from deck.
        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString(entity.Comp.DrawText),
            Act = () => TryDraw(entity, user),
            Priority = 100,
        });
    }

    private bool TryDraw(Entity<PlayingCardDeckComponent> entity, EntityUid user)
    {
        var singleTakenCard = Take(entity, ^1.., Transform(user).Coordinates, user).FirstOrNull();
        if (singleTakenCard is { } card)
        {
            _hands.TryPickupAnyHand(user, card, animate: false);
            return true;
        }

        return false;
    }

    private void OnGetAlternativeVerbsDeck(
        Entity<PlayingCardDeckComponent> entity,
        ref GetVerbsEvent<AlternativeVerb> args
    )
    {
        OnGetAlternativeVerbsStack(entity, ref args);

        if (!args.CanAccess ||
            !args.CanInteract ||
            args.Hands == null)
            return;

        var user = args.User;
        if (entity.Comp.NumCards > 1 && _hands.CanPickupAnyHand(args.User, entity))
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Act = () => CutDeck(entity, user),
                Text = Loc.GetString(entity.Comp.CutText),
                Icon = entity.Comp.CutIcon,
                Priority = 98,
            });
        }
    }

    private void CutDeck(Entity<PlayingCardDeckComponent> entity, EntityUid user)
    {
        _audio.PlayPredicted(entity.Comp.PickUpSound, entity, user);

        var spawned = PredictedSpawnAtPosition(CardDeckEntId, Transform(entity).Coordinates);
        var deck = new Entity<PlayingCardDeckComponent>(spawned, Comp<PlayingCardDeckComponent>(spawned));
        deck.Comp.Prototype = entity.Comp.Prototype;
        if (!IsClientSide(spawned))
        {
            // Can't insert real cards into predicted decks.
            Transfer(entity, deck, ^(entity.Comp.NumCards / 2).., user);
        }

        _hands.TryPickupAnyHand(user, spawned);
    }

    /// Conceptually, this "instantiates" the <see cref="PlayingCardDeckPrototype.Cards">elements</see> in the given
    /// <paramref name="deckId"/>, handling calculating localization strings, sprite layers, etc. Note that this <b>does
    /// not</b> spawn any entities immediately.
    private IEnumerable<PlayingCardInDeck> GetCards(ProtoId<PlayingCardDeckPrototype> deckId)
    {
        if (!_proto.Resolve(deckId, out var deck))
            return [];

        return deck.Cards.SelectMany(deckEl => deckEl switch
        {
            PlayingCardDeckPrototypeElementCard card =>
                Enumerable.Repeat(new PlayingCardInDeckUnspawnedData(card, deck, suit: null), card.Count),
            PlayingCardDeckPrototypeElementPrototypeReference protoRef =>
                Enumerable.Repeat(
                    new PlayingCardInDeckUnspawnedRef(protoRef.Prototype, protoRef.FaceDown),
                    protoRef.Count
                ),
            PlayingCardDeckPrototypeElementSuit s => _proto.Resolve(s.Suit, out var suit)
                ? suit.Cards.SelectMany(suitEl =>
                    Enumerable.Repeat(new PlayingCardInDeckUnspawnedData(suitEl, deck, suit), suitEl.Count)
                )
                : [],
            _ => deckEl.ThrowUnknownInheritor<PlayingCardDeckPrototype.Element, IEnumerable<PlayingCardInDeck>>(),
        });
    }
}
