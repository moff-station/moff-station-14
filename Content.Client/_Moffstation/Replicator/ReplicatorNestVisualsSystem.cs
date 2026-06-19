using Content.Shared._Impstation.Replicator;
using Content.Shared._Moffstation.Replicator.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Moffstation.Replicator;

/// Maintains sprite layers for the replicator nest based on its size.
public sealed partial class ReplicatorNestVisualizerSystem : VisualizerSystem<ReplicatorNestComponent>
{
    protected override void OnAppearanceChange(
        EntityUid uid,
        ReplicatorNestComponent component,
        ref AppearanceChangeEvent args
    )
    {
        var sprite = new Entity<SpriteComponent?>(uid, args.Sprite);
        SetLayerVisible(sprite, args.Component, ReplicatorNestVisualsKeys.Key);
        SetLayerVisible(sprite, args.Component, ReplicatorNestVisualsKeys.KeyUnshaded);
    }

    private void SetLayerVisible(Entity<SpriteComponent?> sprite, AppearanceComponent appearance, Enum key)
    {
        if (AppearanceSystem.TryGetData<ReplicatorNestVisuals>(
                sprite,
                key,
                out var visuals,
                appearance
            ))
        {
            SpriteSystem.LayerSetVisible(sprite, visuals, true);
        }
    }
}
