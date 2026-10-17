using Content.Client.Items.Systems;
using Content.Shared._Moffstation.BladeServer;
using Content.Shared.Hands;
using Robust.Client.GameObjects;
using Robust.Shared.Reflection;

namespace Content.Client._Moffstation.BladeServer;

/// <summary>
/// This is a <see cref="VisualizerSystem{T}"/> for entities with <see cref="BladeServerComponent"/>. It basically just
/// makes the stripe a color or invisible if a color isn't specified.
/// </summary>
public sealed partial class BladeServerVisualizerSystem : VisualizerSystem<BladeServerComponent>
{
    [Dependency] private IReflectionManager _reflect = default!;

    private string StripeKey => field ??= _reflect.GetEnumReference(BladeServerVisuals.StripeLayer);

    protected override void OnAppearanceChange(
        EntityUid uid,
        BladeServerComponent component,
        ref AppearanceChangeEvent args
    )
    {
        if (args.Sprite is not { } spriteComp)
            return;
        var sprite = new Entity<SpriteComponent?>(uid, spriteComp);

        if (AppearanceSystem.TryGetData<Color>(
                uid,
                BladeServerVisuals.StripeColor,
                out var stripeColor,
                args.Component
            ))
        {
            SpriteSystem.LayerSetColor(sprite, BladeServerVisuals.StripeLayer, stripeColor);
            SpriteSystem.LayerSetVisible(sprite, BladeServerVisuals.StripeLayer, true);
        }
        else
        {
            SpriteSystem.LayerSetVisible(sprite, BladeServerVisuals.StripeLayer, false);
        }
    }

    [SubscribeLocalEvent(after: [typeof(ItemSystem)])]
    private void OnGetInhandVisuals(Entity<BladeServerComponent> entity, ref GetInhandVisualsEvent args)
    {
        foreach (var (key, layer) in args.Layers)
        {
            if (key != StripeKey)
                continue;

            layer.Visible = entity.Comp.StripeColor != null;
            layer.Color = entity.Comp.StripeColor;
        }
    }
}
