using System.Numerics;
using Content.Client._Moffstation.Administration.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Moffstation.Administration.Systems;

public sealed class PeakSignSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PeakSignComponent, ComponentStartup>(PeakSignAdded);
        SubscribeLocalEvent<PeakSignComponent, ComponentShutdown>(PeakSignRemoved);
    }

    private void PeakSignRemoved(EntityUid uid, PeakSignComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((uid, sprite), PeakSignKey.Key, out var layer, false))
            return;

        _sprite.RemoveLayer((uid, sprite), layer);
    }

    private void PeakSignAdded(EntityUid uid, PeakSignComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((uid, sprite), PeakSignKey.Key, out var _, false))
            return;

        var adj = _sprite.GetLocalBounds((uid, sprite)).Height / 2 + ((1.0f / 32) * 6.0f);

        var layer = _sprite.AddLayer((uid, sprite), new SpriteSpecifier.Rsi(new ResPath("_Moffstation/Objects/Misc/peaksign.rsi"), "sign"));
        _sprite.LayerMapSet((uid, sprite), PeakSignKey.Key, layer);

        _sprite.LayerSetOffset((uid, sprite), layer, new Vector2(0.0f, adj));
        sprite.LayerSetShader(layer, "unshaded");
    }

    private enum PeakSignKey
    {
        Key,
    }
}
