using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.SprayPainter.GasTanks;
using Robust.Client.GameObjects;
using Robust.Shared.Reflection;

namespace Content.Client.SprayPainter.GasTanks;

public sealed partial class GasTankVisualsSystem : VisualizerSystem<GasTankVisualsComponent>
{
    [Dependency] private readonly IReflectionManager _reflect = default!;
    [Dependency] private readonly SharedItemSystem _itemSys = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasTankVisualsComponent, GetInhandVisualsEvent>(OnGetHeldVisuals,
            after: [typeof(ItemSystem)]);
        SubscribeLocalEvent<GasTankVisualsComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals,
            after: [typeof(ClientClothingSystem)]);
    }

    protected override void OnAppearanceChange(EntityUid uid,
        GasTankVisualsComponent component,
        ref AppearanceChangeEvent args
    )
    {
        if (args.Sprite is not { } sprite)
            return;

        if (AppearanceSystem.TryGetData<Color>(
                uid,
                GasTankVisualsLayers.Tank,
                out var tank,
                args.Component
            ))
        {
            // This should always be set, so no visibility toggling.
            sprite.LayerSetColor(GasTankVisualsLayers.Tank, tank);
        }

        if (AppearanceSystem.TryGetData<Color>(
                uid,
                GasTankVisualsLayers.StripeMiddle,
                out var middleStripe,
                args.Component
            ))
        {
            sprite.LayerSetVisible(GasTankVisualsLayers.StripeMiddle, true);
            sprite.LayerSetColor(GasTankVisualsLayers.StripeMiddle, middleStripe);
        }
        else
        {
            sprite.LayerSetVisible(GasTankVisualsLayers.StripeMiddle, false);
        }

        if (AppearanceSystem.TryGetData<Color>(
                uid,
                GasTankVisualsLayers.StripeLow,
                out var lowerStripe,
                args.Component
            ))
        {
            sprite.LayerSetVisible(GasTankVisualsLayers.StripeLow, true);
            sprite.LayerSetColor(GasTankVisualsLayers.StripeLow, lowerStripe);
        }
        else
        {
            sprite.LayerSetVisible(GasTankVisualsLayers.StripeLow, false);
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
