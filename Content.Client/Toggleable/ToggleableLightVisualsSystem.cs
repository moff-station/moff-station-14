using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Toggleable;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using System.Linq;
using Content.Shared.Light.Components; // Moffstation

namespace Content.Client.Toggleable;

/// <summary>
/// Implements the behavior of <see cref="ToggleableLightVisualsComponent"/> by reacting to
/// <see cref="AppearanceChangeEvent"/>, for the sprite directly; <see cref="OnGetHeldVisuals"/> for the
/// in-hand visuals; and <see cref="OnGetEquipmentVisuals"/> for the clothing visuals.
/// </summary>
/// <see cref="ToggleableLightVisualsComponent"/>
public sealed class ToggleableLightVisualsSystem : VisualizerSystem<ToggleableLightVisualsComponent>
{
    [Dependency] private readonly SharedItemSystem _itemSys = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ToggleableLightVisualsComponent, GetInhandVisualsEvent>(OnGetHeldVisuals, after: new[] { typeof(ItemSystem) });
        SubscribeLocalEvent<ToggleableLightVisualsComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals, after: new[] { typeof(ClientClothingSystem) });
    }

    protected override void OnAppearanceChange(EntityUid uid, ToggleableLightVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<bool>(uid, ToggleVisuals.Toggled, out var enabled, args.Component)) // Moffstation - ToggleableLightVisuals enum merged into ToggleVisuals
            return;

        var modulate = AppearanceSystem.TryGetData<Color>(uid, ToggleVisuals.Color, out var color, args.Component); // Moffstation - ToggleableLightVisuals enum merged into ToggleVisuals

        // Update the item's sprite
        if (args.Sprite != null && component.SpriteLayer != null && args.Sprite.LayerMapTryGet(component.SpriteLayer, out var layer))
        {
            args.Sprite.LayerSetVisible(layer, enabled);
            if (modulate)
                args.Sprite.LayerSetColor(layer, color);
        }

        // Update any point-lights
        if (TryComp(uid, out PointLightComponent? light) &&
            TryComp<ItemTogglePointLightComponent>(uid, out var toggleLights)) // Moffstation - Use `ItemTogglePointLightComponent`
        {
            DebugTools.Assert(!light.NetSyncEnabled, $"{typeof(ItemTogglePointLightComponent)} requires point lights without net-sync"); // Moffstation - Use `ItemTogglePointLightComponent`
            _lights.SetEnabled(uid, enabled, light);
            if (toggleLights.ToggleVisualsColorModulatesLights && modulate) // Moffstation - Use `ItemTogglePointLightComponent`
            {
                _lights.SetColor(uid, color, light);
            }
        }

        // update clothing & in-hand visuals.
        _itemSys.VisualsChanged(uid);
    }

    /// <summary>
    ///     Add the unshaded light overlays to any clothing sprites.
    /// </summary>
    private void OnGetEquipmentVisuals(EntityUid uid, ToggleableLightVisualsComponent component, GetEquipmentVisualsEvent args)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance)
            || !AppearanceSystem.TryGetData<bool>(uid, ToggleVisuals.Toggled, out var enabled, appearance) // Moffstation - ToggleableLightVisuals enum merged into ToggleVisuals
            || !enabled)
            return;

        if (!TryComp(args.Equipee, out InventoryComponent? inventory))
            return;
        List<PrototypeLayerData>? layers = null;

        // attempt to get species specific data
        if (inventory.SpeciesId != null)
            component.ClothingVisuals.TryGetValue($"{args.Slot}-{inventory.SpeciesId}", out layers);

        // No species specific data.  Try to default to generic data.
        if (layers == null && !component.ClothingVisuals.TryGetValue(args.Slot, out layers))
            return;

        var modulate = AppearanceSystem.TryGetData<Color>(uid, ToggleVisuals.Color, out var color, appearance); // Moffstation - ToggleableLightVisuals enum merged into ToggleVisuals

        var i = 0;
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = i == 0 ? $"{args.Slot}-toggle" : $"{args.Slot}-toggle-{i}";
                i++;
            }

            if (modulate)
                layer.Color = color;

            args.Layers.Add((key, layer));
        }
    }

    private void OnGetHeldVisuals(EntityUid uid, ToggleableLightVisualsComponent component, GetInhandVisualsEvent args)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance)
            || !AppearanceSystem.TryGetData<bool>(uid, ToggleVisuals.Toggled, out var enabled, appearance) // Moffstation - ToggleableLightVisuals enum merged into ToggleVisuals
            || !enabled)
            return;

        if (!component.InhandVisuals.TryGetValue(args.Location, out var layers))
            return;

        var modulate = AppearanceSystem.TryGetData<Color>(uid, ToggleVisuals.Color, out var color, appearance); // Moffstation - ToggleableLightVisuals enum merged into ToggleVisuals

        var i = 0;
        var defaultKey = $"inhand-{args.Location.ToString().ToLowerInvariant()}-toggle";
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = i == 0 ? defaultKey : $"{defaultKey}-{i}";
                i++;
            }

            if (modulate)
                layer.Color = color;

            args.Layers.Add((key, layer));
        }
    }
}
