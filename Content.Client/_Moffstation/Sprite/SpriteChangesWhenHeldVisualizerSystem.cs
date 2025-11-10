using Content.Shared._Moffstation.Sprite;
using Robust.Client.GameObjects;

namespace Content.Client._Moffstation.Sprite;

/// This visualizer system implements the sprite changing behavior of <see cref="SpriteChangesWhenHeldComponent"/>.
public sealed partial class SpriteChangesWhenHeldVisualizerSystem : VisualizerSystem<SpriteChangesWhenHeldComponent>
{
    protected override void OnAppearanceChange(EntityUid uid,
        SpriteChangesWhenHeldComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite ||
            !AppearanceSystem.TryGetData<bool>(uid, SpriteChangesWhenHeldVisuals.IsHeld, out var isHeld))
            return;

        var entity = new Entity<SpriteComponent?>(uid, sprite);
        var layers = isHeld ? component.HeldLayers : component.NotHeldLayers;
        foreach (var (layer, state) in layers)
        {
            SpriteSystem.LayerSetData(entity, layer, state);
        }
    }
}
