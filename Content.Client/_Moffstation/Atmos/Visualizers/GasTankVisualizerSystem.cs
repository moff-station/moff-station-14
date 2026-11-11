using System.Linq;
using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared._Moffstation.Atmos.Components;
using Content.Shared._Moffstation.Atmos.Visuals;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Shared.Reflection;

namespace Content.Client._Moffstation.Atmos.Visualizers;

/// <summary>
/// A <see cref="GasHolderVisualizerSystem{T}"/> for <see cref="GasCanVisualsComponent"/>.
/// </summary>
public sealed partial class GasCanVisualizerSystem : GasHolderVisualizerSystem<GasCanVisualsComponent>;

/// <summary>
/// A <see cref="GasHolderVisualizerSystem{T}"/> for <see cref="GasTankVisualsComponent"/>.
/// Handles inhand, clothing, and storage visuals.
/// </summary>
public sealed partial class GasTankVisualizerSystem : GasHolderVisualizerSystem<GasTankVisualsComponent>
{
    [SubscribeLocalEvent]
    private void OnGetStoredVisuals(Entity<GasTankVisualsComponent> entity, ref GetStoredVisualsEvent args)
    {
        if (!entity.Comp.HasStoredSprite)
            return;

        OnGetGenericVisuals(
            entity,
            args.Layers,
            "stored",
            key => $"stored-{LayerToRsiState(key)}",
            includeHardware: true
        );
    }

    [SubscribeLocalEvent(after: [typeof(ItemSystem)])]
    private void OnGetHeldVisuals(Entity<GasTankVisualsComponent> entity, ref GetInhandVisualsEvent args)
    {
        // Copy location because the lambda below doesn't want to capture over a `ref` field.
        var location = args.Location;
        OnGetGenericVisuals(
            entity,
            args.Layers,
            $"hand-{args.Location.ToString().ToLowerInvariant()}",
            // Return null if this layer should be excluded.
            key => entity.Comp.ExcludedInhandLayers.Contains(key) ? null : GetInhandRsiState(key, location)
        );
    }

    [SubscribeLocalEvent(after: [typeof(ClientClothingSystem)])]
    private void OnGetEquipmentVisuals(Entity<GasTankVisualsComponent> entity, ref GetEquipmentVisualsEvent args)
    {
        // If the component says this species uses different clothing, pass in the species ID.
        string? species = null;
        if (TryComp<InventoryComponent>(args.Equipee, out var inventory) &&
            inventory.SpeciesId is { } speciesId &&
            entity.Comp.SpeciesWithDifferentClothing.Contains(speciesId))
            species = speciesId;

        // Copy slot because the lambda below doesn't want to capture over a `ref` field.
        var slot = args.Slot;
        OnGetGenericVisuals(
            entity,
            args.Layers,
            $"equipped-{args.Slot.ToUpperInvariant()}-",
            key => GetEquippedRsiState(key, slot, species)
        );
    }

    private static string? GetInhandRsiState(GasHolderVisualsLayers layer, HandLocation hand) =>
        LayerToRsiState(layer) is { } state ? $"inhand-{hand.ToString().ToLowerInvariant()}-{state}" : null;

    private static string? GetEquippedRsiState(GasHolderVisualsLayers layer, string inventorySlot, string? species)
    {
        if (LayerToRsiState(layer) is not { } state)
            return null;

        string slotStr;
        switch (inventorySlot)
        {
            case "suitstorage":
                slotStr = "SUITSTORAGE";
                break;
            case "belt":
                slotStr = "BELT";
                break;
            case "back":
                slotStr = "BACKPACK";
                break;
            default:
                return null;
        }

        var speciesSuffix = species != null ? $"-{species.ToLowerInvariant()}" : "";
        return $"equipped-{slotStr}-{state}{speciesSuffix}";
    }
}

/// <summary>
/// This <see cref="VisualizerSystem{T}"/> manages gas holders' visuals as described by <see cref="GasHolderVisualsLayers"/>
/// and <see cref="GasTankVisualsComponent"/>.
/// </summary>
public abstract partial class GasHolderVisualizerSystem<T> : VisualizerSystem<T>
    where T : Component, IGasHolderVisualsComponent
{
    [Dependency] private IReflectionManager _reflect = default!;
    [Dependency] private SharedItemSystem _item = default!;


    private readonly IReadOnlyList<GasHolderVisualsLayers> _modifiableLayers = new List<GasHolderVisualsLayers>
        { GasHolderVisualsLayers.Tank, GasHolderVisualsLayers.StripeMiddle, GasHolderVisualsLayers.StripeLow };

    protected override void OnAppearanceChange(
        EntityUid uid,
        T component,
        ref AppearanceChangeEvent args
    )
    {
        if (args.Sprite is not { } sprite)
            return;

        var entity = new Entity<AppearanceComponent, SpriteComponent>(uid, args.Component, sprite);
        foreach (var layer in _modifiableLayers)
        {
            SetLayerVisibilityAndColor(entity, layer);
        }

        // update clothing & in-hand visuals.
        _item.VisualsChanged(uid);
    }

    private void SetLayerVisibilityAndColor(
        Entity<AppearanceComponent, SpriteComponent> entity,
        GasHolderVisualsLayers layer
    )
    {
        var sprite = new Entity<SpriteComponent?>(entity, entity.Comp2);
        if (AppearanceSystem.TryGetData<Color>(
                entity,
                layer,
                out var color,
                entity
            ))
        {
            SpriteSystem.LayerSetVisible(sprite, layer, true);
            SpriteSystem.LayerSetColor(sprite, layer, color);
        }
        else
        {
            SpriteSystem.LayerSetVisible(sprite, layer, false);
        }
    }

    protected void OnGetGenericVisuals(
        Entity<GasTankVisualsComponent> entity,
        List<(string, PrototypeLayerData)> layers,
        string visualKeyPrefix,
        Func<GasHolderVisualsLayers, string?> visualsLayerToRsiState,
        bool includeHardware = false
    )
    {
        if (!TryComp<AppearanceComponent>(entity, out var appearance))
            return;

        foreach (var key in _modifiableLayers)
        {
            if (visualsLayerToRsiState(key) is not { } state)
                continue;

            var hasAppearance = AppearanceSystem.TryGetData<Color>(entity, key, out var color, appearance);
            layers.Add((
                $"{visualKeyPrefix}-{_reflect.GetEnumReference(key)}",
                new PrototypeLayerData
                {
                    State = state,
                    Visible = hasAppearance,
                    Color = color,
                }
            ));
        }

        if (includeHardware && visualsLayerToRsiState(GasHolderVisualsLayers.Hardware) is { } hardwareState)
        {
            layers.Add((
                $"{visualKeyPrefix}-{_reflect.GetEnumReference(GasHolderVisualsLayers.Hardware)}",
                new PrototypeLayerData { State = hardwareState }
            ));
        }
    }

    protected static string? LayerToRsiState(GasHolderVisualsLayers layer) => layer switch
    {
        GasHolderVisualsLayers.Hardware => "hardware",
        GasHolderVisualsLayers.Tank => "tank",
        GasHolderVisualsLayers.StripeMiddle => "stripe-middle",
        GasHolderVisualsLayers.StripeLow => "stripe-low",
        _ => null,
    };
}
