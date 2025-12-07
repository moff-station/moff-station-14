using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Cards.Events;

/// This is raised on an entity when a card parented to it is flipped. This is useful for informing card stacks that
/// they need to update their visuals because a card's sprite has changed.
[ByRefEvent]
public record struct ContainedPlayingCardFlippedEvent;

/// This is raised on a card when it is flipped.
[ByRefEvent]
public record struct PlayingCardFlippedEvent;

/// This even is raised on a card stack when its contents are changed.
[ByRefEvent]
public record struct PlayingCardStackContentsChangedEvent(StackQuantityChangeType Type, EntityUid? User);

/// The type of change of a <see cref="PlayingCardStackContentsChangedEvent"/>
[Serializable, NetSerializable]
public enum StackQuantityChangeType : sbyte
{
    Added,
    Removed,
}

/// A message sent from the UI of <see cref="Components.PlayingCardHandComponent"/> indicating the actor wants to remove
/// the card specified by <see cref="Card"/> from it.
[Serializable, NetSerializable]
public sealed class DrawPlayingCardFromHandMessage(NetEntity card) : BoundUserInterfaceMessage
{
    public NetEntity Card = card;
}
