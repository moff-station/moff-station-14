using System.Linq;
using System.Numerics;
using Content.Client._Moffstation.GameObjects;
using Content.Shared._Moffstation.Cards.Components;
using Content.Shared._Moffstation.Cards.Systems;
using Robust.Client.GameObjects;

namespace Content.Client._Moffstation.Cards;

public sealed partial class CardDeckVisualizerSystem : ManagedLayerVisualizerSystem<PlayingCardDeckComponent>
{
    [Dependency] private readonly SharedPlayingCardsSystem _playingCards = default!;

    protected override ref HashSet<string> SpriteLayersAdded(PlayingCardDeckComponent component) =>
        ref component.SpriteLayersAdded;

    protected override void AddLayersOnAppearanceChange(
        PlayingCardDeckComponent component,
        Entity<SpriteComponent?> sprite,
        AppearanceComponent appearance,
        Func<string, PrototypeLayerData, SpriteComponent.Layer> layerFactory
    )
    {
        if (!AppearanceSystem.TryGetData<List<PlayingCardInDeck>>(
                sprite,
                PlayingCardStackVisuals.Cards,
                out var visibleCards,
                appearance
            ))
            return;

        var layerRotation = Angle.FromDegrees(90);
        var layerScale = new Vector2(component.Scale, component.Scale);
        foreach (var (cardIndex, cardInDeck) in visibleCards.Index())
        {
            if (_playingCards.GetComponent(cardInDeck)?.Sprite() is not { } currentLayers)
                continue;

            foreach (var (cardLayerIndex, cardLayerData) in currentLayers.Index())
            {
                var layer = layerFactory($"{cardIndex}-{cardLayerIndex}", cardLayerData);
                SpriteSystem.LayerSetRotation(layer, layerRotation + (cardLayerData.Rotation ?? 0));
                SpriteSystem.LayerSetScale(layer, layerScale * (cardLayerData.Scale ?? Vector2.One));
                SpriteSystem.LayerSetOffset(
                    layer,
                    new Vector2(0, component.YOffset * cardIndex) + (cardLayerData.Offset ?? Vector2.Zero)
                );
            }
        }
    }
}
