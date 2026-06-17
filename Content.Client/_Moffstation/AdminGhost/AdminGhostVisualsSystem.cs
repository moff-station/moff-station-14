using Content.Shared._Moffstation.AdminGhost;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;

namespace Content.Client._Moffstation.AdminGhost;

public sealed partial class AdminGhostVisualsSystem : EntitySystem
{
    [Dependency] private ISerializationManager _serialization = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IResourceCache _resourceCache = default!;

    private SpriteSystem _spriteSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        _spriteSys = EntityManager.System<SpriteSystem>();
        SubscribeLocalEvent<AdminGhostCustomizationComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
    }

    private void OnAfterHandleState(Entity<AdminGhostCustomizationComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Log.Debug($"AdminGhostVisuals OnAfterHandleState for {ent.Owner}, SpritePrototype={ent.Comp.SpritePrototype}");
        ApplySpriteFromPrototype(ent.Owner, ent.Comp.SpritePrototype);
    }

    private void ApplySpriteFromPrototype(EntityUid uid, EntProtoId? protoId)
    {
        if (protoId == null)
        {
            Log.Debug($"AdminGhostVisuals: protoId is null, restoring default sprite");
            RestoreDefaultSprite(uid);
            return;
        }

        Log.Debug($"AdminGhostVisuals: applying sprite from prototype '{protoId}' to entity {uid}");

        if (!_prototype.TryIndex<EntityPrototype>(protoId.Value, out var proto))
        {
            Log.Debug($"AdminGhostVisuals: prototype '{protoId}' not found");
            return;
        }

        SetSpriteFromProto(uid, proto);
        SetGenericVisualizerFromProto(uid, proto);

        if (TryComp<AppearanceComponent>(uid, out var appearance))
            _appearance.QueueUpdate(uid, appearance);

        Log.Debug($"AdminGhostVisuals: sprite application complete for {uid}");
    }

    private void SetSpriteFromProto(EntityUid uid, EntityPrototype proto)
    {
        if (!proto.TryGetComponent(out SpriteComponent? src, EntityManager.ComponentFactory))
        {
            Log.Debug($"AdminGhostVisuals: prototype '{proto.ID}' has no SpriteComponent");
            return;
        }

        RemComp<SpriteComponent>(uid);
        var sprite = AddComp<SpriteComponent>(uid);

        if (!proto.Components.TryGetValue("Sprite", out var entry))
            return;

        // Set BaseRSI if the prototype specifies a component-level sprite: path.
        if (entry.Mapping.TryGetValue("sprite", out var spriteNode)
            && spriteNode is ValueDataNode spriteVal
            && !string.IsNullOrWhiteSpace(spriteVal.Value))
        {
            var rsiPath = SpriteSystem.TextureRoot / spriteVal.Value;
            if (_resourceCache.TryGetResource<RSIResource>(rsiPath, out var rsiRes))
                _spriteSys.SetBaseRsi((uid, sprite), rsiRes!.RSI);
        }

        // Read raw layer data from the prototype YAML to preserve all fields:
        // MapKeys, Shader, Offset, Rotation, RenderingStrategy, etc.
        if (entry.Mapping.TryGetValue("layers", out var layersNode)
            && layersNode is SequenceDataNode layersList)
        {
            foreach (var layerNode in layersList)
            {
                if (layerNode is not MappingDataNode layerMapping)
                    continue;

                var layerData = _serialization.Read<PrototypeLayerData>(layerMapping, notNullableOverride: true);
                _spriteSys.AddLayer((uid, sprite), layerData, null);
            }
            return;
        }

        // No explicit layers — use BaseRSI if it was set above.
        if (sprite.BaseRSI != null)
        {
            foreach (var state in sprite.BaseRSI)
            {
                _spriteSys.AddRsiLayer((uid, sprite), state.StateId);
                break;
            }
        }
    }

    private void SetGenericVisualizerFromProto(EntityUid uid, EntityPrototype proto)
    {
        if (proto.TryGetComponent(out GenericVisualizerComponent? src, EntityManager.ComponentFactory))
        {
            RemComp<GenericVisualizerComponent>(uid);
            var dest = AddComp<GenericVisualizerComponent>(uid);
            _serialization.CopyTo(src, ref dest, notNullableOverride: true);
            Dirty(uid, dest);
        }
        else if (HasComp<GenericVisualizerComponent>(uid))
        {
            RemComp<GenericVisualizerComponent>(uid);
        }
    }

    private void RestoreDefaultSprite(EntityUid uid)
    {
        if (!TryComp<MetaDataComponent>(uid, out var meta))
        {
            Log.Debug($"AdminGhostVisuals: cannot restore - no MetaDataComponent on {uid}");
            return;
        }

        var proto = meta.EntityPrototype;
        if (proto == null)
        {
            Log.Debug($"AdminGhostVisuals: cannot restore - no EntityPrototype on {uid}");
            return;
        }

        Log.Debug($"AdminGhostVisuals: restoring default sprite from '{proto.ID}' for {uid}");

        // Walk the prototype inheritance chain to find one with a SpriteComponent,
        // since the entity's direct prototype (e.g. AdminObserver) inherits it
        // from a parent (e.g. MobObserverBase).
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (proto != null)
        {
            if (!visited.Add(proto.ID))
                break;

            if (proto.TryGetComponent(out SpriteComponent? _, EntityManager.ComponentFactory))
            {
                SetSpriteFromProto(uid, proto);
                SetGenericVisualizerFromProto(uid, proto);

                if (TryComp<AppearanceComponent>(uid, out var appearance))
                    _appearance.QueueUpdate(uid, appearance);

                return;
            }

            var parentList = proto.Parents;
            if (parentList is { Length: > 0 })
            {
                var parentId = parentList[0];
                if (!_prototype.TryIndex<EntityPrototype>(parentId, out proto))
                    break;
            }
            else
            {
                proto = null;
            }
        }

        Log.Debug($"AdminGhostVisuals: no parent prototype found with a SpriteComponent for '{meta.EntityPrototype?.ID}'");
    }
}
