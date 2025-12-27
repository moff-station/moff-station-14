using Content.Shared._Moffstation.Cards.Components;
using Content.Shared._Moffstation.Cards.Systems;

namespace Content.Server._Moffstation.Cards;

public sealed class PlayingCardsSystem : SharedPlayingCardsSystem
{
    protected override void ForceAppearanceUpdate(Entity<PlayingCardComponent> card)
    {
        // No appearance updates on the server.
    }
}
