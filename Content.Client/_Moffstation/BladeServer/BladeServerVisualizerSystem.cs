using Content.Shared._Moffstation.BladeServer;
using Robust.Client.GameObjects;

namespace Content.Client._Moffstation.BladeServer;

/// <summary>
/// This is a <see cref="VisualizerSystem{T}"/> for entities with <see cref="BladeServerComponent"/>. It basically just
/// makes the stripe a color or invisible if a color isn't specified.
/// </summary>
public sealed partial class BladeServerVisualizerSystem : VisualizerSystem<BladeServerComponent>
{
    protected override void OnAppearanceChange(
        EntityUid uid,
        BladeServerComponent component,
        ref AppearanceChangeEvent args
    )
    {
        if (args.Sprite is not { } sprite)
            return;

        var entity = new Entity<SpriteComponent?, AppearanceComponent>(uid, sprite, args.Component);
        if (AppearanceSystem.TryGetData<Color>(entity, BladeServerVisuals.StripeColor, out var stripeColor, entity))
        {
            SpriteSystem.LayerSetColor(entity, BladeServerVisuals.StripeLayer, stripeColor);
            SpriteSystem.LayerSetVisible(entity, BladeServerVisuals.StripeLayer, true);
        }
        else
        {
            SpriteSystem.LayerSetVisible(entity, BladeServerVisuals.StripeLayer, false);
        }
    }
}
