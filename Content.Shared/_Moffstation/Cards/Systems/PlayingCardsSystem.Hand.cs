using System.Linq;
using Content.Shared._Moffstation.Cards.Components;
using Content.Shared._Moffstation.Cards.Events;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

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
        SubscribeLocalEvent<PlayingCardHandComponent, ContainedPlayingCardFlippedEvent>(DirtyVisuals);
        SubscribeLocalEvent<PlayingCardHandComponent, DrawPlayingCardFromHandMessage>(OnDrawPlayingCardFromHand);
        SubscribeLocalEvent<PlayingCardHandComponent, PlayingCardPickedEvent>(OnPlayingCardPicked);
        SubscribeLocalEvent<PlayingCardHandComponent, PlayingCardStackContentsChangedEvent>(OnCardStackQuantityChange);
        SubscribeLocalEvent<PlayingCardHandComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PlayingCardHandComponent, GetVerbsEvent<UtilityVerb>>(OnGetUtilityVerbsStack);
        SubscribeLocalEvent<PlayingCardHandComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);

        SubscribeLocalEvent<HandsComponent, PlayingCardPickedEvent>(OnPlayingCardPicked);
    }

    /// Gets all the cards in the given hand, or an empty enumerable if the entity is not a hand of cards.
    public IEnumerable<Entity<PlayingCardComponent>> GetCards(Entity<PlayingCardHandComponent?> entity)
    {
        if (IsClientSide(entity) ||
            !Resolve(entity, ref entity.Comp))
            return [];

        return entity.Comp.Cards.Select(NetEntToCardOrNull).OfType<Entity<PlayingCardComponent>>();
    }

    /// Opens <paramref name="hand"/>'s picker UI for <paramref name="user"/>. The picked card will be given to
    /// <paramref name="destination"/>, which means different things depending on the components on that
    /// entity.
    /// <seealso cref="OnDrawPlayingCardFromHand"/>
    public void OpenPickerUi(Entity<PlayingCardHandComponent> hand, EntityUid destination, EntityUid user)
    {
        hand.Comp.PickedCardDestination = GetNetEntity(destination);
        Dirty(hand);
        _ui.OpenUi(hand.Owner, PlayingCardHandUiKey.Key, user);
    }

    /// Creates a new hand from the given cards. Returns null if no cards were given. Returns an entity which may
    /// be predicted.
    /// <remarks>
    /// As of original writing, this function is only ever used by <see cref="JoinIntoHeldCardMakingHand"/>, as hands of
    /// cards are always made by combining cards in a user's hands. This function is here and public for completeness of
    /// the system's API.
    /// </remarks>
    /// <seealso cref="CreateDeckPredicted"/>
    public Entity<PlayingCardHandComponent> CreateHandPredicted(
        IEnumerable<Entity<PlayingCardComponent>> cards,
        EntityCoordinates spawnAt,
        EntityUid? user
    ) => PredictedCreateStack(CardHandEntId, spawnAt, cards, user);

    /// This function helps with handling <see cref="PlayingCardPickedEvent"/> by <see cref="Take">taking</see> the
    /// card specified by <paramref name="args"/> from the hand specified in the event.
    public Entity<PlayingCardComponent>
        TakePickedCard(PlayingCardPickedEvent args, EntityCoordinates destinationCoords) =>
        Take(args.SourceHand, args.Range, destinationCoords, args.User).Single();


    /// Handles <see cref="DrawPlayingCardFromHandMessage"/> which is sent from the <see cref="PlayingCardHandComponent"/>
    /// picker UI by raising a <see cref="PlayingCardPickedEvent"/> on the destination entity in the event.
    /// <seealso cref="OpenPickerUi"/>
    private void OnDrawPlayingCardFromHand(
        Entity<PlayingCardHandComponent> sourceHand,
        ref DrawPlayingCardFromHandMessage args
    )
    {
        var index = sourceHand.Comp.Cards.IndexOf(args.Card);
        if (index == -1)
        {
            this.AssertOrLogError(
                $"Received a message to draw a card that that isn't in the hand. (card={args.Card}, hand={sourceHand})."
            );
            return;
        }

        var destination = GetEntity(sourceHand.Comp.PickedCardDestination) ?? args.Actor;
        sourceHand.Comp.PickedCardDestination = null;
        Dirty(sourceHand);

        var ev = new PlayingCardPickedEvent(sourceHand, index..(index + 1), args.Actor);
        RaiseLocalEvent(destination, ref ev);
    }

    /// Takes and tries to pick up the card specified by <paramref name="args"/>.
    private void OnPlayingCardPicked(Entity<HandsComponent> entity, ref PlayingCardPickedEvent args)
    {
        if (args.Handled)
            return;

        // Dumb special case for when somebody hits "E" on the hand in their active hand.
        if (_hands.TryGetActiveItem(entity.AsNullable(), out var activeItem) && activeItem == args.SourceHand)
        {
            _hands.TryPickupAnyHand(
                entity,
                TakePickedCard(args, Transform(entity).Coordinates),
                animate: false,
                handsComp: entity
            );
        }
        else
        {
            var pickedUp = _hands.TryPickup(
                entity,
                TakePickedCard(args, Transform(entity).Coordinates),
                handId: null,
                animate: false,
                handsComp: entity
            );
            if (!pickedUp)
            {
                this.AssertOrLogError("Failed to pick up picked card");
            }
        }

        args.Handled = true;
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
                    StackQuantityChangeType.Added => PlayingCardHandComponent.CardsAddedText,
                    StackQuantityChangeType.Removed => PlayingCardHandComponent.CardsRemovedText,
                    _ => this.AssertOrLogError($"Unknown variant of {nameof(StackQuantityChangeType)}: {args.Type}",
                        PlayingCardHandComponent.CardsChangedText),
                },
                ("quantity", entity.Comp.NumCards)
            ),
            entity,
            args.User
        );
    }


    /// These interactions are invoked when left-clicking on a hand with a held item.
    private void OnInteractUsing(Entity<PlayingCardHandComponent> targetHand, ref InteractUsingEvent args)
    {
        if (args.Handled || args.Used == args.Target)
            return;

        OpenPickerUi(targetHand, args.Used, args.User);
        args.Handled = true;
    }

    /// These verbs are available when right-clicking on a hand.
    private void OnGetAlternativeVerbs(
        Entity<PlayingCardHandComponent> targetHand,
        ref GetVerbsEvent<AlternativeVerb> args
    )
    {
        OnGetAlternativeVerbsStack(targetHand, ref args);

        if (!args.CanAccess ||
            !args.CanInteract ||
            args.Hands == null)
            return;

        var user = args.User;
        args.Verbs.Add(PlayingCardHandComponent.Verbs.ConvertToDeck, () => ConvertToDeck(targetHand, user));
    }


    private void ConvertToDeck(Entity<PlayingCardHandComponent> hand, EntityUid user)
    {
        if (_hands.IsHolding(user, hand, out var handId))
        {
            // It's gonna get deleted anyway, so drop it so that we can pick up the spawned deck immediately.
            _hands.TryDrop(user, hand, checkActionBlocker: false, doDropInteraction: false);
        }

        var spawned = CreateDeckPredicted([], user, null);
        if (!IsClientSide(spawned))
        {
            // Server can insert the cards into the deck.
            var deck = new Entity<PlayingCardDeckComponent>(spawned, Comp<PlayingCardDeckComponent>(spawned));
            Transfer(hand, deck, .., user);
        }

        if (handId is not null)
        {
            _hands.TryPickup(user, spawned, handId, animate: false);
        }
    }

    /// Creates a new hand composed of <paramref name="heldCard"/> and <paramref name="cardsToAdd"/>. It is required
    /// that <paramref name="user"/> be holding <paramref name="heldCard"/> in one of its
    /// <see cref="HandsComponent">hands</see>, as that hand is where the new hand of playing cards will be spawned.
    /// <seealso cref="JoinIntoHandIfHeldOtherwiseDeck"/>
    private Entity<PlayingCardHandComponent>? JoinIntoHeldCardMakingHand(
        EntityUid user,
        Entity<PlayingCardComponent> heldCard,
        IEnumerable<Entity<PlayingCardComponent>> cardsToAdd
    )
    {
        Entity<HandsComponent?> userHands = user;
        if (!_hands.IsHolding(userHands, heldCard, out var handId))
        {
            this.AssertOrLogError($"Expected {user} to be holding {heldCard}");
            return null;
        }

        if (!_hands.TryDrop(userHands, handId))
            return null;

        var newHand = CreateHandPredicted([heldCard, ..cardsToAdd], Transform(user).Coordinates, user);
        if (!_hands.TryPickup(user, newHand, handId, animate: false, handsComp: userHands))
        {
            this.AssertOrLogError($"{user} failed to pick up newly spawned hand of cards to expected hand");
        }

        return newHand;
    }
}
