using Content.Shared._Moffstation.Extensions;
using Robust.Client.GameObjects;
using Robust.Shared.Reflection;

namespace Content.Client._Moffstation.Visuals;

/// This system implements the behavior of <see cref="SynchronizeLayerColorToAppearanceComponent"/>
public sealed partial class SynchronizeLayerColorToAppearanceSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IReflectionManager _reflection = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;
    [Dependency] private EntityQuery<SynchronizeLayerColorToAppearanceComponent> _synchQuery;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SynchronizeLayerColorToAppearanceComponent> entity, ref MapInitEvent args)
    {
        // This mostly exists to cause test failures when this component is used incorrectly.
        EnsureComp<AppearanceComponent>(entity);
    }

    [SubscribeLocalEvent]
    private void OnAppearanceChange(
        Entity<SynchronizeLayerColorToAppearanceComponent> entity,
        ref AppearanceChangeEvent args
    )
    {
        if (_appearanceQuery.ResolveOrNull(entity) is not { } appearance ||
            _spriteQuery.ResolveOrNull(entity, logMissing: false) is not { } s)
            return;

        var sprite = s.AsNullable();
        foreach (var (layerKey, appearanceKey) in entity.Comp.Layers)
        {
            if (SharedAppearanceSystemExt.GetColorOrNull(_appearance, appearance, appearanceKey) is not { } color ||
                !_sprite.TryGetLayer(sprite, layerKey, out var layer, logMissing: false) &&
                (!_reflection.TryParseEnumReference(layerKey, out var enumKey) ||
                 !_sprite.TryGetLayer(sprite, enumKey, out layer, logMissing: true)))
                continue;

            _sprite.LayerSetColor(layer, color);
        }
    }

    public void SynchronizeLayerColors(
        Entity<SynchronizeLayerColorToAppearanceComponent?> entity,
        string slot,
        ref List<(string, PrototypeLayerData)> layersAndKey
    )
    {
        if (_synchQuery.ResolveOrNull(entity, logMissing: false) is not { } synch ||
            !synch.Comp.ClothingLayers.TryGetValue(slot, out var synchLayers) ||
            _appearanceQuery.ResolveOrNull(entity) is not { } appearance)
            return;

        foreach (var (layerKey, layer) in layersAndKey)
        {
            if (!synchLayers.TryGetValue(layerKey, out var appearanceKey) ||
                _appearance.GetColorOrNull(appearance, appearanceKey) is not { } color)
                continue;

            layer.Color = color;
        }
    }
}

static file partial class SharedAppearanceSystemExt
{
    extension(SharedAppearanceSystem system)
    {
        public Color? GetColorOrNull(Entity<AppearanceComponent> entity, Enum key)
        {
            if (system.TryGetData<Color>(entity, key, out var color, entity) ||
                system.TryGetData<string>(entity, key, out var colorStr, entity) &&
                Color.TryParse(colorStr, out color))
                return color;

            return null;
        }
    }
}
