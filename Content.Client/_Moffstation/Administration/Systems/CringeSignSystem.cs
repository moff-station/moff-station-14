using System.Numerics;
using Content.Client._Moffstation.Administration.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Moffstation.Administration.Systems;

public sealed class CringeSignSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CringeSignComponent, ComponentStartup>(CringeSignAdded);
        SubscribeLocalEvent<CringeSignComponent, ComponentShutdown>(CringeSignRemoved);
    }

    private void CringeSignRemoved(EntityUid uid, CringeSignComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((uid, sprite), CringeSignKey.Key, out var layer, false))
            return;

        _sprite.RemoveLayer((uid, sprite), layer);
    }

    private void CringeSignAdded(EntityUid uid, CringeSignComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((uid, sprite), CringeSignKey.Key, out var _, false))
            return;

        var adj = _sprite.GetLocalBounds((uid, sprite)).Height / 2 + ((1.0f / 32) * 6.0f);

        var layer = _sprite.AddLayer((uid, sprite), new SpriteSpecifier.Rsi(new ResPath("_Moffstation/Objects/Misc/cringesign.rsi"), "sign"));
        _sprite.LayerMapSet((uid, sprite), CringeSignKey.Key, layer);

        _sprite.LayerSetOffset((uid, sprite), layer, new Vector2(0.0f, adj));
        sprite.LayerSetShader(layer, "unshaded");
    }

    private enum CringeSignKey
    {
        Key,
    }
}
