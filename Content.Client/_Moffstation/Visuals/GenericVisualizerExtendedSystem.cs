using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;

namespace Content.Client._Moffstation.Visuals;

/// Implements the behavior of <see cref="GenericVisualizerComponent"/>. Analogous to <see cref="GenericVisualizerSystem"/>.
public sealed partial class GenericVisualizerExtendedSystem : VisualizerSystem<GenericVisualizerExtendedComponent>
{
    /// Used as a default in certain places to simplify logic.
    private static readonly Dictionary<Enum, Dictionary<string, Dictionary<string, PrototypeLayerData>>> Empty = new();

    [Dependency] private SharedItemSystem _itemSys = default!;
    [Dependency] private SharedAppearanceSystem _appearanceSys = default!;

    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        // The `after`s here are to make sure we get the default layers first, so that we can override them.

        SubscribeLocalEvent<GenericVisualizerExtendedComponent, EntGotInsertedIntoContainerMessage>(
            OnEntGotInsertedIntoContainer);
        SubscribeLocalEvent<GenericVisualizerExtendedComponent, GetInhandVisualsEvent>(OnGetHeldVisuals,
            after: [typeof(ItemSystem)]);
        SubscribeLocalEvent<GenericVisualizerExtendedComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals,
            after: [typeof(ClientClothingSystem)]);
        SubscribeLocalEvent<GenericVisualizerExtendedComponent, GetStoredVisualsEvent>(OnGetStoredVisuals);
    }

    protected override void OnAppearanceChange(
        EntityUid uid,
        GenericVisualizerExtendedComponent component,
        ref AppearanceChangeEvent args
    )
    {
        // Force other visuals to change when appearance data changes.
        _itemSys.VisualsChanged(uid);
    }

    private void OnEntGotInsertedIntoContainer(
        Entity<GenericVisualizerExtendedComponent> entity,
        ref EntGotInsertedIntoContainerMessage args
    )
    {
        // Force other visuals to change when put into storage. Realistically, this is used to immediately update
        // storage visuals for items which spawn inside of inventories.
        _itemSys.VisualsChanged(entity);
    }

    private void OnGetStoredVisuals(Entity<GenericVisualizerExtendedComponent> entity, ref GetStoredVisualsEvent args)
    {
        AddLayersFromVisuals(entity.Owner, entity.Comp.StoredVisuals, ref args.Layers);
    }

    private void OnGetHeldVisuals(Entity<GenericVisualizerExtendedComponent> entity, ref GetInhandVisualsEvent args)
    {
        AddLayersFromVisuals(
            entity.Owner,
            entity.Comp.InhandVisuals.GetValueOrDefault(args.Location, Empty),
            ref args.Layers
        );
    }

    private void OnGetEquipmentVisuals(
        Entity<GenericVisualizerExtendedComponent> entity,
        ref GetEquipmentVisualsEvent args
    )
    {
        AddLayersFromVisuals(
            entity.Owner,
            entity.Comp.ClothingVisuals.GetValueOrDefault(args.Slot, Empty),
            ref args.Layers
        );
    }

    /// Adds/overwrites visuals from <paramref name="visuals"/> into <paramref name="layers"/>. The actual visuals used
    /// are based on <paramref name="entity"/>'s <see cref="AppearanceComponent">appearance data</see>.
    private void AddLayersFromVisuals(
        Entity<AppearanceComponent?> entity,
        Dictionary<Enum, Dictionary<string, Dictionary<string, PrototypeLayerData>>> visuals,
        ref List<(string, PrototypeLayerData)> layers
    )
    {
        foreach (var layer in GetLayersFromVisuals(entity.Owner, visuals))
        {
            // "Overwrite" existing layers with the same key.
            layers.RemoveAll(key => key.Item1 == layer.Item1);
            layers.Add(layer);
        }
    }

    /// Returns the "active" layers from <paramref name="visuals"/> based on <paramref name="entity"/>'s
    /// <see cref="AppearanceComponent">appearance data</see>.
    private IEnumerable<(string, PrototypeLayerData)> GetLayersFromVisuals(
        Entity<AppearanceComponent?> entity,
        Dictionary<Enum, Dictionary<string, Dictionary<string, PrototypeLayerData>>> visuals
    )
    {
        if (!_appearanceQuery.Resolve(entity, ref entity.Comp))
            yield break;

        foreach (var (appearanceKey, layerDict) in visuals)
        {
            if (!_appearanceSys.TryGetData(entity, appearanceKey, out var obj, entity))
                continue;

            var appearanceValue = obj.ToString();
            if (string.IsNullOrEmpty(appearanceValue))
                continue;

            foreach (var (layerKeyRaw, layerDataDict) in layerDict)
            {
                if (!layerDataDict.TryGetValue(appearanceValue, out var layerData))
                    continue;

                yield return (layerKeyRaw, layerData);
            }
        }
    }
}
