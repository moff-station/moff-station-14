using System.Linq;
using Content.Shared._Moffstation.Cards.Components;
using Content.Shared._Moffstation.Cards.Events;
using Content.Shared._Moffstation.Cards.Prototypes;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Random.Helpers;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
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
        SubscribeLocalEvent<PlayingCardDeckComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<PlayingCardDeckComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PlayingCardDeckComponent, PlayingCardStackContentsChangedEvent>(DirtyVisuals);
        SubscribeLocalEvent<PlayingCardDeckComponent, ContainedPlayingCardFlippedEvent>(DirtyVisuals);
        SubscribeLocalEvent<PlayingCardDeckComponent, ExaminedEvent>(OnExaminedDeck);
        SubscribeLocalEvent<PlayingCardDeckComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbsDeck);
        SubscribeLocalEvent<PlayingCardDeckComponent, InteractUsingEvent>(OnInteractUsing);
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

    private void OnInteractHand(Entity<PlayingCardDeckComponent> entity, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        var singleTakenCard = Take(entity, ^1.., Transform(args.User).Coordinates, args.User).FirstOrNull();
        if (singleTakenCard is not { } card)
            return;

        _hands.TryPickupAnyHand(args.User, card, animate: false);
        args.Handled = true;
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

    private void OnGetAlternativeVerbsDeck(
        Entity<PlayingCardDeckComponent> entity,
        ref GetVerbsEvent<AlternativeVerb> args
    )
    {
        OnGetAlternativeVerbsCommon(entity, ref args);

        if (!args.CanAccess ||
            !args.CanInteract ||
            args.Hands == null)
            return;

        var user = args.User;

        if (entity.Comp.NumCards > 1)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Act = () => Split(entity, user),
                Text = Loc.GetString(entity.Comp.SplitText),
                Icon = entity.Comp.SplitIcon,
                Priority = 4,
            });
        }

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => Shuffle(entity, user),
            Text = Loc.GetString(entity.Comp.ShuffleText),
            Icon = entity.Comp.ShuffleIcon,
            Priority = 3,
        });
    }

    private void Split(Entity<PlayingCardDeckComponent> entity, EntityUid user)
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


    private void Shuffle(Entity<PlayingCardDeckComponent> entity, EntityUid? user)
    {
        var rand = new System.Random(
            SharedRandomExtensions.HashCodeCombine((int)_gameTiming.CurTick.Value, entity.Owner.Id));
        rand.Shuffle(entity.Comp.Cards);
        Dirty(entity);
        entity.Comp.DirtyVisuals = true;

        _audio.PlayPredicted(entity.Comp.ShuffleSound, entity, user, AudioVariation);
        _popup.PopupPredicted(Loc.GetString("card-verb-shuffle-success", ("target", MetaData(entity).EntityName)),
            entity,
            user
        );
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
