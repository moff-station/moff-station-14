using Content.Shared._Moffstation.Cards.Systems;
using Content.Shared._Moffstation.Extensions;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Cards.Components;

/// This abstract class contains fields shared by <see cref="PlayingCardDeckComponent"/> and
/// <see cref="PlayingCardHandComponent"/>.
[Access(typeof(SharedPlayingCardsSystem))]
public abstract partial class PlayingCardStackComponent : Component, ISealedInheritance
{
    [DataField]
    public SoundSpecifier ShuffleSound = new SoundCollectionSpecifier("cardFan");

    [DataField]
    public SoundSpecifier PickUpSound = new SoundCollectionSpecifier("cardSlide");

    [DataField]
    public SoundSpecifier PlaceDownSound = new SoundCollectionSpecifier("cardShove");

    /// The ID of <see cref="Container"/>.
    [DataField]
    public string ContainerId = "playing-card-stack-container";

    /// The container which holds the card entities in this stack.
    [ViewVariables]
    public Container Container = default!;

    /// The number of cards in this stack.
    [ViewVariables]
    public abstract int NumCards { get; }

    /// This field indicates whether the visuals of this stack need to be updated. This is used to avoid repeatedly
    /// updating visuals on the same stack in a single frame.
    /// <see cref="SharedPlayingCardsSystem.Update"/>
    public bool DirtyVisuals = true;

    /// The maximum number of cards which will be included in the visuals of this stack.
    [DataField(required: true)]
    public int VisualLimit;

    [DataField]
    public LocId JoinText = "card-verb-join";

    [DataField]
    public LocId OrganizeDownText = "cards-verb-organize-down";

    [DataField]
    public LocId OrganizeUpText = "cards-verb-organize-up";

    [DataField]
    public SpriteSpecifier? FlipCardsIcon =
        new SpriteSpecifier.Texture(new ResPath("Interface/VerbIcons/refresh.svg.192dpi.png"));

    [DataField]
    public SpriteSpecifier? JoinIcon =
        new SpriteSpecifier.Texture(new ResPath("Interface/VerbIcons/refresh.svg.192dpi.png"));
}

[Serializable, NetSerializable]
public enum PlayingCardStackVisuals : sbyte
{
    /// This key for appearance data indicates which cards are visible in a stack. It is expected to key a value of type
    /// <c>List&lt;<see cref="PlayingCardInDeck"/>&gt;</c>.
    Cards,
}
