using Content.Shared._Moffstation.Cards.Prototypes;
using Content.Shared._Moffstation.Cards.Systems;
using Content.Shared._Moffstation.Extensions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Cards.Components;

/// A collection of <see cref="PlayingCardComponent">playing cards</see>. Note that because decks of cards can contain
/// many tens of entities, the implementation aggressively tries to <see cref="PlayingCardInDeck">lazily instantiate the cards
/// contained within</see>.
/// <seealso cref="SharedPlayingCardsSystem"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedPlayingCardsSystem))]
public sealed partial class PlayingCardDeckComponent : PlayingCardStackComponent
{
    /// The cards in this deck. Order is important, and is such that the first card in the list is on the bottom of the
    /// deck, and the last card in the list is on the top of the deck. This means that the push/pop behavior of
    /// interacting with the deck should be minimally deleterious to performance.
    [DataField, AutoNetworkedField]
    public List<PlayingCardInDeck> Cards = [];

    /// How many cards are in <see cref="Cards"/>.
    public override int NumCards => Cards.Count;

    /// The card at the top of the deck, that is the one which would be drawn next, or the one whose sprite is most
    /// visible.
    public PlayingCardInDeck? TopCard => NumCards > 0 ? Cards[^1] : null;

    /// The prototype of this deck. This is used to define the contents of a deck in YAML. Note that this is nullable as
    /// arbitrary cards which are not from the same original deck can be joined to create a deck later.
    /// contained in <see cref="Cards"/>.
    [DataField]
    public ProtoId<PlayingCardDeckPrototype>? Prototype;

    /// The visual offset between individual cards when constructing a sprite for this deck based on its contents.
    [DataField]
    public float YOffset = 0.02f;

    /// The visual scale of cards when constructing a sprite for this deck based on its contents.
    [DataField]
    public float Scale = 1;

    [DataField]
    public SpriteSpecifier? SplitIcon =
        new SpriteSpecifier.Texture(new ResPath("Interface/VerbIcons/dot.svg.192dpi.png"));

    [DataField]
    public SpriteSpecifier? ShuffleIcon =
        new SpriteSpecifier.Texture(new ResPath("Interface/VerbIcons/die.svg.192dpi.png"));

    [DataField]
    public SpriteSpecifier? FlipCardsIcon =
        new SpriteSpecifier.Texture(new ResPath("Interface/VerbIcons/refresh.svg.192dpi.png"));

    [DataField]
    public LocId TopCardExamineLoc = "cards-deck-top-card-examine";

    /// Sprite layers added to this entity based on contained cards' <see cref="PlayingCardComponent.Sprite"/>.
    [ViewVariables]
    public HashSet<string> SpriteLayersAdded = [];
}

/// A type representing a card in a <see cref="PlayingCardDeckComponent"/>. This may be an entity, or it may be
/// information about a card which has not yet been spawned.
[ImplicitDataRecord, Serializable, NetSerializable]
public abstract record PlayingCardInDeck : ISealedInheritance
{
    // Private constructor to seal inheritance.
    private PlayingCardInDeck() { }

    /// An existing entity. This SHOULD always have a <see cref="PlayingCardComponent"/> on it.
    [DataRecord, Serializable, NetSerializable]
    public sealed record NetEnt([field: ViewVariables] NetEntity Ent) : PlayingCardInDeck;

    /// An unspawned <see cref="PlayingCardDeckPrototypeElementPrototypeReference"/>.
    [DataRecord, Serializable, NetSerializable]
    public sealed record UnspawnedRef(
        [field: DataField(required: true)] EntProtoId<PlayingCardComponent> Prototype,
        [field: DataField] bool FaceDown
    ) : PlayingCardInDeck;

    /// An unspawned <see cref="PlayingCardDeckPrototypeElementCard"/>.
    [DataRecord, Serializable, NetSerializable]
    public sealed record UnspawnedData(
        [field: DataField(required: true)] PlayingCardDeckPrototypeElementCard Card,
        [field: DataField(required: true)] ProtoId<PlayingCardDeckPrototype> Deck,
        [field: DataField] ProtoId<PlayingCardSuitPrototype>? Suit
    ) : PlayingCardInDeck;
}
