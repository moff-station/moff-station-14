using System.Linq;
using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared._Moffstation.Atmos.Components;
using Content.Shared._Moffstation.Atmos.Visuals;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Shared.Reflection;

namespace Content.Client._Moffstation.Atmos.Visualizers;

/// <summary>
/// This <see cref="VisualizerSystem{T}"/> manages gas tanks' visuals as described by <see cref="GasTankVisualsLayers"/>
/// and <see cref="GasTankVisualsComponent"/>.
/// </summary>
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
        foreach (var layer in GasTankVisualsLayersExtensions.ModifiableLayers)
        {
            SetLayerVisibilityAndColor(entity, layer);
        }

        // update clothing & in-hand visuals.
        _itemSys.VisualsChanged(uid);
    }

    private void SetLayerVisibilityAndColor(
        Entity<AppearanceComponent, SpriteComponent> entity,
        GasTankVisualsLayers layer
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

    private void OnGetHeldVisuals(Entity<GasTankVisualsComponent> entity, ref GetInhandVisualsEvent args)
    {
        // Copy location because the lambda below doesn't want to capture over a `ref` field.
        var location = args.Location;
        OnGetGenericVisuals(
            entity,
            args.Layers,
            $"hand-{args.Location.ToString().ToLowerInvariant()}",
            // Return null if this layer should be excluded.
            key => entity.Comp.ExcludedInhandLayers.Contains(key) ? null : key.RsiStateInHand(location));
    }

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
            key => key.RsiStateEquipped(slot, species)
        );
    }

    private void OnGetGenericVisuals(
        Entity<GasTankVisualsComponent> entity,
        List<(string, PrototypeLayerData)> layers,
        string visualKeyPrefix,
        Func<GasTankVisualsLayers, string?> visualsLayerToRsiState
    )
    {
        if (!TryComp<AppearanceComponent>(entity, out var appearance))
            return;

        foreach (var key in GasTankVisualsLayersExtensions.ModifiableLayers)
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
    }
}
