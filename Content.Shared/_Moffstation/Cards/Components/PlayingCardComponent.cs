using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Cards.Components;

/// A playing card which can be flipped, inserted into a hand, or joined into a deck.
/// <seealso cref="Systems.PlayingCardsSystem"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(Systems.PlayingCardsSystem))]
public sealed partial class PlayingCardComponent : Component
{
    /// The sprite layers of the face, or front, of the card.
    [DataField(required: true), AutoNetworkedField]
    public PrototypeLayerData[] ObverseSprite;

    /// The sprite layers of the back of the card.
    [DataField(required: true), AutoNetworkedField]
    public PrototypeLayerData[] ReverseSprite;

    /// The current sprite layers (based on <see cref="FaceDown"/>), or the sprite layers for the given
    /// <paramref name="faceDownOverride"/>.
    [Access(Other = AccessPermissions.ReadExecute)] // Pure function, I don't care if you execute it.
    public PrototypeLayerData[] Sprite(bool? faceDownOverride = null) => faceDownOverride ?? FaceDown
        ? ReverseSprite
        : ObverseSprite;

    /// Sprite layers added to this entity based on <see cref="Sprite"/>.
    /// This is used by the client visualizer system to correctly remove added layers when the card is flipped.
    [ViewVariables]
    public HashSet<string> SpriteLayersAdded = [];

    /// Is the card facing down, ie. which side is visible. If true, the <see cref="ReverseSprite"/> is visible.
    [DataField, AutoNetworkedField]
    public bool FaceDown;

    /// The name of this card, visible when not face down.
    [DataField(required: true), AutoNetworkedField]
    public LocId Name;

    /// The description of this card, visible when not face down.
    [DataField(required: true), AutoNetworkedField]
    public LocId Description;

    /// The name which will be applied to this entity when it is flipped face down.
    [DataField, AutoNetworkedField]
    public LocId ReverseName = "card-name-reverse";

    /// The description which will be applied to this entity when it is flipped face down.
    [DataField, AutoNetworkedField]
    public LocId? ReverseDescription;

    /// The icon for the "flip" verb on this card.
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? FlipIcon =
        new SpriteSpecifier.Texture(new ResPath("Interface/VerbIcons/flip.svg.192dpi.png"));
}

/// The key used to access appearance data for <see cref="PlayingCardComponent"/>.
/// <seealso cref="AppearanceComponent"/>
[Serializable, NetSerializable]
public enum PlayingCardVisuals : sbyte
{
    IsFaceDown,
}
