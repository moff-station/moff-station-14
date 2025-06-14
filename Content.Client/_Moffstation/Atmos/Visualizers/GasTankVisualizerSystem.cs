using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared._Moffstation.Atmos.Components;
using Content.Shared._Moffstation.Atmos.Visuals;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Shared.Reflection;

namespace Content.Client._Moffstation.Atmos.Visualizers;

public sealed partial class GasTankVisualizerSystem : VisualizerSystem<GasTankVisualsComponent>
{
    [Dependency] private readonly IReflectionManager _reflect = default!;
    [Dependency] private readonly SharedItemSystem _itemSys = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasTankVisualsComponent, GetInhandVisualsEvent>(
            OnGetHeldVisuals,
            after: [typeof(ItemSystem)]
        );
        SubscribeLocalEvent<GasTankVisualsComponent, GetEquipmentVisualsEvent>(
            OnGetEquipmentVisuals,
            after: [typeof(ClientClothingSystem)]
        );
    }

    protected override void OnAppearanceChange(
        EntityUid uid,
        GasTankVisualsComponent component,
        ref AppearanceChangeEvent args
    )
    {
        if (args.Sprite is not { } sprite)
            return;

        var entity = new Entity<AppearanceComponent, SpriteComponent>(uid, args.Component, sprite);
        SetLayerVisibilityAndColor(entity, GasTankVisualsLayers.Tank);
        SetLayerVisibilityAndColor(entity, GasTankVisualsLayers.StripeMiddle);
        SetLayerVisibilityAndColor(entity, GasTankVisualsLayers.StripeLow);

        // update clothing & in-hand visuals.
        _itemSys.VisualsChanged(uid);
    }

    private void SetLayerVisibilityAndColor(Entity<AppearanceComponent, SpriteComponent> entity,
        GasTankVisualsLayers layer)
    {
        var sprite = new Entity<SpriteComponent?>(entity, entity.Comp2);
        if (AppearanceSystem.TryGetData<Color>(
                entity,
                layer,
                out var middleStripe,
                entity
            ))
        {
            SpriteSystem.LayerSetVisible(sprite, layer, true);
            SpriteSystem.LayerSetColor(sprite, layer, middleStripe);
        }
        else
        {
            SpriteSystem.LayerSetVisible(sprite, layer, false);
        }
    }

    private void OnGetHeldVisuals(Entity<GasTankVisualsComponent> entity, ref GetInhandVisualsEvent args)
    {
        OnGetGenericVisuals(entity, args.Layers);
    }

    private void OnGetEquipmentVisuals(Entity<GasTankVisualsComponent> entity, ref GetEquipmentVisualsEvent args)
    {
        OnGetGenericVisuals(entity, args.Layers);
    }

    private void OnGetGenericVisuals(
        Entity<GasTankVisualsComponent> entity,
        List<(string, PrototypeLayerData)> layers)
    {
        if (!TryComp<AppearanceComponent>(entity, out var appearance))
            return;

        // Try to get appearance data for each layer in the sprite, setting the layer's visibility and color based on
        // the appearance data.
        foreach (var (layerKey, layer) in layers)
        {
            if (!_reflect.TryParseEnumReference(layerKey, out var key))
                continue;

            var hasAppearance = AppearanceSystem.TryGetData<Color>(entity, key, out var color, appearance);
            layer.Visible = hasAppearance;
            layer.Color = color;
        }
    }
}
