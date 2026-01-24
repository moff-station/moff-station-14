using Content.Shared._Moffstation.Cards.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Cards.Components;

/// A collection of <see cref="PlayingCardComponent">playing cards</see> which are more accessible and interactible than
/// a <see cref="PlayingCardDeckComponent"/>. Unlike decks, cards in hands are always entities.
/// <seealso cref="SharedPlayingCardsSystem"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedPlayingCardsSystem))]
public sealed partial class PlayingCardHandComponent : PlayingCardStackComponent
{
    /// The cards in this hand.
    [AutoNetworkedField]
    public List<NetEntity> Cards = [];

    /// The number of cards in this hand.
    public override int NumCards => Cards.Count;

    /// When constructing the sprite for this hand based on its contents, this is the total angle across which cards are
    /// spread.
    [DataField]
    public float Angle = 120f;

    /// When constructing the sprite for this hand based on its contents, this is kinda like the radius of the arc
    /// across which the cards are spread.
    [DataField]
    public float XOffset = 0.5f;

    /// The visual scale of cards when constructing a sprite for this hand based on its contents.
    [DataField]
    public float Scale = 1;

    [DataField] public LocId ConvertToDeckText = "card-verb-convert-to-deck";
    [DataField] public SpriteSpecifier? ConvertToDeckIcon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/rotate_cw.svg.192dpi.png"));
    [DataField] public LocId CardsAddedText = "cards-stackquantitychange-added";
    [DataField] public LocId CardsRemovedText = "cards-stackquantitychange-removed";
    [DataField] public LocId CardsChangedText = "cards-stackquantitychange-unknown";

    /// Sprite layers added to this entity based on contained cards' <see cref="PlayingCardComponent.Sprite"/>.
    [ViewVariables]
    public HashSet<string> SpriteLayersAdded = [];
}

/// The value used to key the UI state for a <see cref="PlayingCardHandComponent"/>.
[Serializable, NetSerializable]
public enum PlayingCardHandUiKey : byte { Key }
