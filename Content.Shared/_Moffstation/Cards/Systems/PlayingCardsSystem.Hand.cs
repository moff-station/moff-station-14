using System.Linq;
using Content.Shared._Moffstation.Cards.Components;
using Content.Shared._Moffstation.Cards.Events;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Cards.Systems;

// This part handles PlayingCardHandComponent.
public abstract partial class SharedPlayingCardsSystem
{
    /// The ID of the entity prototype which is used to construct hands dynamically.
    private static readonly EntProtoId<PlayingCardHandComponent> CardHandEntId = "PlayingCardHandDynamic";

    private void InitHand()
    {
        SubscribeLocalEvent<PlayingCardHandComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PlayingCardHandComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PlayingCardHandComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PlayingCardHandComponent, GetVerbsEvent<UtilityVerb>>(OnGetUtilityVerbsStack);
        SubscribeLocalEvent<PlayingCardHandComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
        SubscribeLocalEvent<PlayingCardHandComponent, DrawPlayingCardFromHandMessage>(OnDrawPlayingCardFromHand);
        SubscribeLocalEvent<PlayingCardHandComponent, PlayingCardStackContentsChangedEvent>(OnCardStackQuantityChange);
        SubscribeLocalEvent<PlayingCardHandComponent, ContainedPlayingCardFlippedEvent>(DirtyVisuals);
    }

    /// Gets all the cards in the given hand, or an empty enumerable if the entity is not a hand of cards.
    public IEnumerable<Entity<PlayingCardComponent>> GetCards(Entity<PlayingCardHandComponent?> entity)
    {
        if (IsClientSide(entity) ||
            !Resolve(entity, ref entity.Comp))
            return [];

        return entity.Comp.Cards.Select(NetEntToCardOrNull).OfType<Entity<PlayingCardComponent>>();
    }

    /// Creates a new hand from the given cards. Returns null if no cards were given. Returns an entity which may
    /// be predicted.
    public EntityUid? CreateHand(IEnumerable<Entity<PlayingCardComponent>> cards, EntityUid? user)
    {
        var cardsList = cards.ToList();
        if (cardsList.FirstOrNull() is not { } firstCard)
            return null;

        var resultingHandCoords = Transform(firstCard).Coordinates;
        var spawned = PredictedSpawnAtPosition(CardHandEntId, resultingHandCoords);
        if (!IsClientSide(spawned))
        {
            // Can't insert cards into a predicted hand.
            var hand = new Entity<PlayingCardHandComponent>(spawned, Comp<PlayingCardHandComponent>(spawned));
            Add(
                hand,
                cardsList.Select(card =>
                {
                    Flip(card, faceDown: false);
                    return card;
                }),
                resultingHandCoords,
                user
            );
        }

        return spawned;
    }


    private void OnCardStackQuantityChange(
        Entity<PlayingCardHandComponent> entity,
        ref PlayingCardStackContentsChangedEvent args
    )
    {
        entity.Comp.DirtyVisuals = true;
        _popup.PopupPredicted(
            Loc.GetString(
                args.Type switch
                {
                    StackQuantityChangeType.Added => entity.Comp.CardsAddedText,
                    StackQuantityChangeType.Removed => entity.Comp.CardsRemovedText,
                    _ => entity.Comp.CardsChangedText,
                },
                ("quantity", entity.Comp.NumCards)
            ),
            entity,
            args.User
        );
    }

    private void OnDrawPlayingCardFromHand(
        Entity<PlayingCardHandComponent> entity,
        ref DrawPlayingCardFromHandMessage args
    )
    {
        var index = entity.Comp.Cards.IndexOf(args.Card);
        if (index == -1)
        {
            this.AssertOrLogError(
                $"Received a message to draw a card that that isn't in the hand. (card={args.Card}, hand={entity})."
            );
            return;
        }

        var card = Take(
                entity,
                index..(index + 1),
                Transform(args.Actor).Coordinates,
                args.Actor
            )
            .Single();
        _hands.TryPickupAnyHand(args.Actor, card, animate: false);
    }

    private void OnGetAlternativeVerbs(
        Entity<PlayingCardHandComponent> entity,
        ref GetVerbsEvent<AlternativeVerb> args
    )
    {
        OnGetAlternativeVerbsStack(entity, ref args);

        var user = args.User;
        // Convert to deck.
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => ConvertToDeck(entity, user),
            Text = Loc.GetString(entity.Comp.ConvertToDeckText),
            Icon = entity.Comp.ConvertToDeckIcon,
        });
    }

    private void ConvertToDeck(Entity<PlayingCardHandComponent> hand, EntityUid? user)
    {
        var spawned = PredictedSpawnAtPosition(CardDeckEntId, Transform(hand).Coordinates);

        var wasHoldingBeforeConversion = false;
        if (user is { } u && _hands.IsHolding(u, hand))
        {
            wasHoldingBeforeConversion = true;
            // It's gonna get deleted anyway, so drop it so that we can pick up the spawned deck immediately.
            _hands.TryDrop(u, hand, checkActionBlocker: false, doDropInteraction: false);
        }

        if (!IsClientSide(spawned))
        {
            // Server can insert the cards into the deck.
            var deck = new Entity<PlayingCardDeckComponent>(spawned, Comp<PlayingCardDeckComponent>(spawned));
            Transfer(hand, deck, .., user);
        }

        if (wasHoldingBeforeConversion)
        {
            _hands.TryPickupAnyHand(user!.Value, spawned);
        }
    }
}
