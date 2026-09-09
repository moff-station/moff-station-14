using Content.Shared._Moffstation.DamageState;
using Content.Shared._Moffstation.Replicator;
using Content.Shared._Moffstation.Replicator.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Moffstation.Replicator;

/// Maintains replicator combat visuals.
public sealed partial class ReplicatorVisualizerSystem : VisualizerSystem<ReplicatorComponent>
{
    protected override void OnAppearanceChange(
        EntityUid uid,
        ReplicatorComponent component,
        ref AppearanceChangeEvent args
    )
    {
        if (args.Sprite == null)
            return;

        var sprite = new Entity<SpriteComponent?>(uid, args.Sprite);
        // make sure we can sync the frames
        if (!SpriteSystem.TryGetLayer(sprite, ReplicatorVisuals.Combat, out var combatLayer, logMissing: true)
            || !SpriteSystem.TryGetLayer(sprite, DamageStateVisualLayers.Base, out var baseLayer, logMissing: true))
            return;

        var combat = AppearanceSystem.TryGetData<bool>(sprite, ReplicatorVisuals.Combat, out var c) && c;

        // then sync them to the base animation
        SpriteSystem.LayerSetVisible(combatLayer, combat);
        SpriteSystem.LayerSetAnimationTime(combatLayer, baseLayer.AnimationTime);
        combatLayer.AnimationFrame = baseLayer.AnimationFrame;
        combatLayer.AnimationTimeLeft = baseLayer.AnimationTimeLeft;
    }
}
