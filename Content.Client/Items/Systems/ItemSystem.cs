using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Moffstation.Extensions; // Moffstation
using Content.Shared.Hands;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map; // Moffstation
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.Items.Systems;

public sealed class ItemSystem : SharedItemSystem
{
    [Dependency] private readonly IResourceCache _resCache = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemComponent, GetInhandVisualsEvent>(OnGetVisuals);

        // TODO is this still needed? Shouldn't containers occlude them?
        SubscribeLocalEvent<SpriteComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<SpriteComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnUnequipped(EntityUid uid, SpriteComponent component, GotUnequippedEvent args)
    {
        _sprite.SetVisible((uid, component), true);
    }

    private void OnEquipped(EntityUid uid, SpriteComponent component, GotEquippedEvent args)
    {
        _sprite.SetVisible((uid, component), false);
    }

    #region InhandVisuals

    /// <summary>
    ///     When an items visual state changes, notify and entities that are holding this item that their sprite may need updating.
    /// </summary>
    public override void VisualsChanged(EntityUid uid)
    {
        // if the item is in a container, it might be equipped to hands or inventory slots --> update visuals.
        if (Container.TryGetContainingContainer((uid, null, null), out var container))
        {
            // Moffstation - Begin - Add appearance data based stored sprites
            if (TryComp<ItemComponent>(uid, out var itemComp))
            {
                // Might be in a storage grid, so update those visuals.
                var ev = new GetStoredVisualsEvent();
                RaiseLocalEvent(uid, ref ev);
                itemComp.StoredLayers = ev.Layers.Count > 0
                    ? ev.Layers.Select(keyAndLayer => (
                            keyAndLayer.Item1,
                            keyAndLayer.Item2.WithUnlessAlreadySpecified(rsiPath: itemComp.RsiPath)
                        ))
                        .ToArray()
                    : null;
            }
            // Moffstation - End

            RaiseLocalEvent(container.Owner, new VisualsChangedEvent(GetNetEntity(uid), container.ID));
        }
    }

    // Moffstation - Begin - Add appearance data based stored sprites
    /// <summary>
    /// Creates or updates <see cref="existingVirtualEntity"/> with the <see cref="ItemComponent.StoredLayers"/> on
    /// <paramref name="item"/>. In the case that no such layers can be retrieved, returns null, indicating the entity's
    /// default sprite should be used instead.
    /// </summary>
    public Entity<SpriteComponent>? SpawnOrUpdateVirtualEntityForInventoryGridStorage(
        Entity<ItemComponent> item,
        Entity<SpriteComponent>? existingVirtualEntity
    )
    {
        if (item.Comp.StoredLayers is not { } storedLayers)
            return null;

        Entity<SpriteComponent> sprite;
        if (existingVirtualEntity is { } existing)
        {
            sprite = existing;
        }
        else
        {
            var virt = Spawn("VirtualItem", MapCoordinates.Nullspace);
            sprite = (virt, EnsureComp<SpriteComponent>(virt));
        }

        var spriteNullable = sprite.AsNullable();
        foreach (var (key, storedLayer) in storedLayers)
        {
            _sprite.RemoveLayer(spriteNullable, key, logMissing: false);

            var layer = _sprite.AddLayer(spriteNullable, storedLayer, index: null);
            _sprite.LayerMapAdd(spriteNullable, key, layer);
        }

        return sprite;
    }
    // Moffstation - End

    /// <summary>
    ///     An entity holding this item is requesting visual information for in-hand sprites.
    /// </summary>
    private void OnGetVisuals(EntityUid uid, ItemComponent item, GetInhandVisualsEvent args)
    {
        var defaultKey = $"inhand-{args.Location.ToString().ToLowerInvariant()}";

        // try get explicit visuals
        if (!item.InhandVisuals.TryGetValue(args.Location, out var layers))
        {
            // get defaults
            if (!TryGetDefaultVisuals(uid, item, defaultKey, out layers))
                return;
        }

        var i = 0;
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = i == 0 ? defaultKey : $"{defaultKey}-{i}";
                i++;
            }

            args.Layers.Add((key, layer));
        }
    }

    /// <summary>
    ///     If no explicit in-hand visuals were specified, this attempts to populate with default values.
    /// </summary>
    /// <remarks>
    ///     Useful for lazily adding in-hand sprites without modifying yaml. And backwards compatibility.
    /// </remarks>
    private bool TryGetDefaultVisuals(EntityUid uid, ItemComponent item, string defaultKey, [NotNullWhen(true)] out List<PrototypeLayerData>? result)
    {
        result = null;

        RSI? rsi = null;

        if (item.RsiPath != null)
            rsi = _resCache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / item.RsiPath).RSI;
        else if (TryComp(uid, out SpriteComponent? sprite))
            rsi = sprite.BaseRSI;

        if (rsi == null)
            return false;

        var state = (item.HeldPrefix == null)
            ? defaultKey
            : $"{item.HeldPrefix}-{defaultKey}";

        if (!rsi.TryGetState(state, out var _))
            return false;

        var layer = new PrototypeLayerData();
        layer.RsiPath = rsi.Path.ToString();
        layer.State = state;
        layer.MapKeys = new() { state };

        result = new() { layer };
        return true;
    }
    #endregion
}

[ByRefEvent]
public record struct GetStoredVisualsEvent()
{
    /// <summary>
    ///     The layers that will be added to the entity that is holding this item.
    /// </summary>
    /// <remarks>
    ///     Note that the actual ordering of the layers depends on the order in which they are added to this list;
    /// </remarks>
    public List<(string, PrototypeLayerData)> Layers = new();
}
