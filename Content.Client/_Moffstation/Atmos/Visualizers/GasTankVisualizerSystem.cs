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
        if (args.Sprite is not { } s)
            return;
        var sprite = new Entity<SpriteComponent?>(uid, s);

        if (AppearanceSystem.TryGetData<Color>(
                uid,
                GasTankVisualsLayers.Tank,
                out var tank,
                args.Component
            ))
        {
            // This should always be set, so no visibility toggling.
            SpriteSystem.LayerSetColor(sprite, GasTankVisualsLayers.Tank, tank);
        }

        if (AppearanceSystem.TryGetData<Color>(
                uid,
                GasTankVisualsLayers.StripeMiddle,
                out var middleStripe,
                args.Component
            ))
        {
            SpriteSystem.LayerSetVisible(sprite, GasTankVisualsLayers.StripeMiddle, true);
            SpriteSystem.LayerSetColor(sprite, GasTankVisualsLayers.StripeMiddle, middleStripe);
        }
        else
        {
            SpriteSystem.LayerSetVisible(sprite, GasTankVisualsLayers.StripeMiddle, false);
        }

        if (AppearanceSystem.TryGetData<Color>(
                uid,
                GasTankVisualsLayers.StripeLow,
                out var lowerStripe,
                args.Component
            ))
        {
            SpriteSystem.LayerSetVisible(sprite, GasTankVisualsLayers.StripeLow, true);
            SpriteSystem.LayerSetColor(sprite, GasTankVisualsLayers.StripeLow, lowerStripe);
        }
        else
        {
            SpriteSystem.LayerSetVisible(sprite, GasTankVisualsLayers.StripeLow, false);
        }

        // update clothing & in-hand visuals.
        _itemSys.VisualsChanged(uid);
    }

    private void OnGetHeldVisuals(Entity<GasTankVisualsComponent> entity, ref GetInhandVisualsEvent args)
    {
        OnGetGenericVisuals(entity, ref args.Layers);
    }

    private void OnGetEquipmentVisuals(Entity<GasTankVisualsComponent> entity, ref GetEquipmentVisualsEvent args)
    {
        OnGetGenericVisuals(entity, ref args.Layers);
    }

    private void OnGetGenericVisuals(
        Entity<GasTankVisualsComponent> entity,
        ref List<(string, PrototypeLayerData)> layers)
    {
        if (!TryComp<AppearanceComponent>(entity, out var appearance))
            return;

        foreach (var (layerKey, layer) in layers)
        {
            if (!_reflect.TryParseEnumReference(layerKey, out var key))
                continue;

            var hasAppearance = AppearanceSystem.TryGetData<Color>(entity, key, out var color, appearance);

            // We only mess with the visibility of stripes.
            if (key is GasTankVisualsLayers.StripeMiddle or GasTankVisualsLayers.StripeLow)
                layer.Visible = hasAppearance;

            layer.Color = color;
        }
    }
}
