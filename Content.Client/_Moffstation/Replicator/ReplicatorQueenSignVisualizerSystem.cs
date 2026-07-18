using Content.Shared._Moffstation.Replicator;
using Content.Shared._Moffstation.Replicator.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Moffstation.Replicator;

/// Maintains a sprite layer to display the "queen sign" or "crown" as provided by <see cref="ReplicatorQueenSignComponent"/>.
public sealed partial class ReplicatorQueenSignVisualizerSystem : VisualizerSystem<ReplicatorQueenSignComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ReplicatorQueenSignComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var sprite = new Entity<SpriteComponent?>(uid, args.Sprite);
        // If queen sign data is set...
        if (AppearanceSystem.TryGetData<PrototypeLayerData>(sprite, ReplicatorVisuals.Queen, out var layerDat))
        {
            // ... make sure the queen sign sprite is visible.
            // Get or add the queen sign layer.
            if (!SpriteSystem.LayerMapTryGet(sprite, ReplicatorVisuals.Queen, out var layer, false))
            {
                layer = SpriteSystem.AddLayer(sprite, layerDat, null);
            }

            SpriteSystem.LayerMapSet(sprite, ReplicatorVisuals.Queen, layer);
            sprite.Comp?.LayerSetShader(layer, "unshaded");
        }
        else if (SpriteSystem.LayerMapTryGet(sprite, ReplicatorVisuals.Queen, out var layer, false))
        {
            // ... otherwise, ensure it's not set.
            SpriteSystem.RemoveLayer(sprite, layer);
        }
    }
}
