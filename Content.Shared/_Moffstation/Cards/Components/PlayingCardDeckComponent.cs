using Content.Shared._Moffstation.Cards.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Cards.Components;

/// A collection of <see cref="PlayingCardComponent">playing cards</see>. Note that because decks of cards can contain
/// many tens of entities, the implementation aggressively tries to <see cref="PlayingCardInDeck">lazily instantiate the cards
/// contained within</see>.
/// <seealso cref="Systems.PlayingCardsSystem"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(PlayingCardsSystem))]
public sealed partial class PlayingCardDeckComponent : PlayingCardStackComponent
{
    /// The cards in this deck. Order is important, and is such that the first card in the list is on the bottom of the
    /// deck, and the last card in the list is on the top of the deck. This means that the push/pop behavior of
    /// interacting with the deck should be minimally deleterious to performance.
    [DataField, AutoNetworkedField]
    public List<PlayingCardInDeck> Cards = [];

    /// How many cards are in <see cref="Cards"/>.
    public override int NumCards => Cards.Count;

    /// The prototype of this deck. This is used to define the contents of a deck in YAML. Note that this is nullable as
    /// arbitrary cards which are not from the same original deck can be joined to create a deck later.
    /// Note also that this is <b>critical</b> to instantiating <see cref="PlayingCardInDeck.Unspawned">unspawned cards</see>
    /// contained in <see cref="Cards"/>.
    [DataField(required: true)]
    public ProtoId<PlayingCardDeckPrototype>? Prototype;

    /// The visual offset between individual cards when constructing a sprite for this deck based on its contents.
    [DataField]
    public float YOffset = 0.02f;

    /// The visual scale of cards when constructing a sprite for this deck based on its contents.
    [DataField]
    public float Scale = 1;

    [DataField] public SpriteSpecifier? SplitIcon =
        new SpriteSpecifier.Texture(new ResPath("Interface/VerbIcons/dot.svg.192dpi.png"));

    [DataField] public SpriteSpecifier? ShuffleIcon =
        new SpriteSpecifier.Texture(new ResPath("Interface/VerbIcons/die.svg.192dpi.png"));

    [DataField] public SpriteSpecifier? FlipCardsIcon =
        new SpriteSpecifier.Texture(new ResPath("Interface/VerbIcons/refresh.svg.192dpi.png"));

    /// Sprite layers added to this entity based on contained cards' <see cref="PlayingCardComponent.CurrentSprite"/>.
    [ViewVariables]
    public HashSet<string> SpriteLayersAdded = [];
}

/// A type representing a card in a <see cref="PlayingCardDeckComponent"/>. This may be an entity, or it may be
/// information about a card which has not yet been spawned.
[ImplicitDataRecord, Serializable, NetSerializable]
public abstract record PlayingCardInDeck
{
    // Private constructor to seal inheritance.
    private PlayingCardInDeck() { }

    /// An existing entity. This SHOULD always have a <see cref="PlayingCardComponent"/> on it.
    [DataRecord, Serializable, NetSerializable]
    public sealed record NetEnt([field: ViewVariables] NetEntity Ent) : PlayingCardInDeck;

    /// An unspawned card.
    /// <seealso cref="PlayingCardDeckPrototypeElement"/>
    [DataRecord, Serializable, NetSerializable]
    public sealed record Unspawned([field: ViewVariables] PlayingCardDeckPrototypeElement Data) : PlayingCardInDeck;
}

/// The description of a deck of cards. This is used to define a collection of cards all at once with minimal
/// duplication in the YAML.
[Prototype]
public sealed partial class PlayingCardDeckPrototype : IPrototype, IInheritingPrototype
{
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<PlayingCardDeckPrototype>))]
    public string[]? Parents { get; private set; }

    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// The RSI which cards in this deck will use by default.
    [DataField("sprite", required: true)]
    public ResPath RsiPath;

    /// Obverse sprite layers which all cards in this deck will have by default. They will be under layers specified on
    /// the card itself.
    [DataField]
    public PrototypeLayerData[] CommonObverseSprite = [];

    /// Reverse sprite layers which all cards in this deck will have by default.
    [DataField(required: true)]
    public PrototypeLayerData[] CommonReverseSprite = default!;

    [DataField(required: true)]
    public string CardLocPrefix = default!;

    [DataField]
    public string LocCardNameSuffix = "name";

    [DataField]
    public string LocCardDescSuffix = "description";

    [DataField]
    public string LocCommonReverseCardId = "reverse";

    public string CardName(string card) => $"{CardLocPrefix}-{card}-{LocCardNameSuffix}";
    public string CardDescription(string card) => $"{CardLocPrefix}-{card}-{LocCardDescSuffix}";

    public string ReverseName() => $"{CardLocPrefix}-{LocCommonReverseCardId}-{LocCardNameSuffix}";
    public string ReverseDescription() => $"{CardLocPrefix}-{LocCommonReverseCardId}-{LocCardDescSuffix}";

    /// The actual cards in this deck.
    /// <seealso cref="PlayingCardDeckPrototypeElement"/>
    [DataField(required: true)]
    public List<PlayingCardDeckPrototypeElement> Cards = [];
}

/// Logically, a card within in a <see cref="PlayingCardDeckPrototype"/>. This can either just defer to an existing entity
/// prototype or be minimal information about a card to be completed by information on the deck prototype. This enables
/// easy definition of cards as part of a deck with lots of shared parts while also enabling an "escape hatch" to say
/// "I don't want anything done for me, just put this existing card prototype in the deck".
[ImplicitDataRecord, Serializable, NetSerializable]
public abstract record PlayingCardDeckPrototypeElement
{
    /// If the card should spawn in the deck facing down.
    [DataField]
    public bool FaceDown;
}

/// A <see cref="PlayingCardDeckPrototypeElement"/> which refers to an existing card prototype. Whatever that prototype is,
/// it'll be stuck in the deck. I hope it has the card component :^)
[DataRecord, Serializable, NetSerializable]
public record PlayingCardDeckPrototypeElementProtoRef : PlayingCardDeckPrototypeElement
{
    public const string PrototypeKey = "prototype";

    [field: DataField(PrototypeKey, required: true)]
    public EntProtoId<PlayingCardComponent> Prototype;
}

/// A <see cref="PlayingCardDeckPrototypeElement"/> which will construct a card entity with defaults specified on the deck
/// and finished by the information in this data definition.
[DataRecord, Serializable, NetSerializable]
public record PlayingCardDeckPrototypeElementData : PlayingCardDeckPrototypeElement
{
    public const string IdKey = "id";

    [DataField(IdKey, required: true)]
    public string Id = default!;

    [DataField]
    public PrototypeLayerData[]? ObverseSprite;
}

/// This custom serializer enables YAMLers to not need to specify the type of cards in the deck prototype, so long
/// as they have the right fields to implicitly discriminate between the variants.
[TypeSerializer]
public sealed class Serializer : ITypeSerializer<PlayingCardDeckPrototypeElement, MappingDataNode>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null
    )
    {
        if (node.Has(PlayingCardDeckPrototypeElementProtoRef.PrototypeKey))
            return serializationManager.ValidateNode<PlayingCardDeckPrototypeElementProtoRef>(node, context);

        if (node.Has(PlayingCardDeckPrototypeElementData.IdKey))
            return serializationManager.ValidateNode<PlayingCardDeckPrototypeElementData>(node, context);

        return new ErrorNode(node, "Custom validation failed! Please specify the type manually!");
    }

    public PlayingCardDeckPrototypeElement Read(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<PlayingCardDeckPrototypeElement>? instanceProvider = null
    )
    {
        if (node.Has(PlayingCardDeckPrototypeElementProtoRef.PrototypeKey))
            return serializationManager.Read<PlayingCardDeckPrototypeElementProtoRef>(node,
                context,
                notNullableOverride: true);

        if (node.Has(PlayingCardDeckPrototypeElementData.IdKey))
            return serializationManager.Read<PlayingCardDeckPrototypeElementData>(node,
                context,
                notNullableOverride: true);

        return (PlayingCardDeckPrototypeElement)serializationManager.Read(
            typeof(PlayingCardDeckPrototypeElement),
            node,
            context,
            notNullableOverride: true
        )!;
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        PlayingCardDeckPrototypeElement value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null
    ) => value switch
    {
        PlayingCardDeckPrototypeElementData cardDeckCardData => Write(
            serializationManager,
            cardDeckCardData,
            dependencies,
            alwaysWrite,
            context
        ),
        PlayingCardDeckPrototypeElementProtoRef cardDeckCardPrototypeRef => Write(
            serializationManager,
            cardDeckCardPrototypeRef,
            dependencies,
            alwaysWrite,
            context
        ),
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };
}
