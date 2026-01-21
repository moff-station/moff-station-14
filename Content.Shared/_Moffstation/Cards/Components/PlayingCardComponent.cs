using Content.Shared._Moffstation.Cards.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Cards.Components;

/// A playing card which can be flipped, inserted into a hand, or joined into a deck.
/// <seealso cref="SharedPlayingCardsSystem"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedPlayingCardsSystem))]
public sealed partial class PlayingCardComponent : Component
{
    /// The sprite layers of the face, or front, of the card.
    [DataField(required: true), AutoNetworkedField]
    public PrototypeLayerData[] ObverseLayers;

    /// The sprite layers of the back of the card.
    [DataField(required: true), AutoNetworkedField]
    public PrototypeLayerData[] ReverseLayers;

    /// The current sprite layers (based on <see cref="FaceDown"/>), or the sprite layers for the given
    /// <paramref name="faceDownOverride"/>.
    [Access(Other = AccessPermissions.ReadExecute)] // Pure function, I don't care if you execute it.
    public PrototypeLayerData[] Sprite(bool? faceDownOverride = null) => faceDownOverride ?? FaceDown
        ? ReverseLayers
        : ObverseLayers;

    /// Sprite layers added to this entity based on <see cref="Sprite"/>.
    /// This is used by the client visualizer system to correctly remove added layers when the card is flipped.
    [ViewVariables]
    public HashSet<string> SpriteLayersAdded = [];

    /// Is the card facing down, ie. which side is visible. If true, the <see cref="ReverseLayers"/> is visible.
    [DataField, AutoNetworkedField]
    public bool FaceDown;

    /// The name of this card, visible when not face down.
    [DataField(required: true), AutoNetworkedField]
    public string Name;

    /// The description of this card, visible when not face down.
    [DataField(required: true), AutoNetworkedField]
    public string Description;

    /// The name which will be applied to this entity when it is flipped face down.
    [DataField(required: true), AutoNetworkedField]
    public string ReverseName;

    /// The description which will be applied to this entity when it is flipped face down.
    [DataField, AutoNetworkedField]
    public string? ReverseDescription;

    /// The localization ID for the "flip" verb on this card.
    [DataField]
    public LocId FlipText = "cards-verb-flip";

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
