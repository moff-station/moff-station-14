using Content.Client._Moffstation.Overlay.Systems;
using Content.Shared._Moffstation.ArInterface.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client._Moffstation.ArInterface.Systems;

public sealed partial class ArInterfaceSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private TransformSystem _xform = default!;
    [Dependency] private IPlayerManager _playerMan = default!;
    [Dependency] private ArInterfaceOverlaySystem _arOverlay = default!;

    public override void Update(float dt)
    {
        if (! _arOverlay.IsActive || _playerMan.LocalEntity is not {} viewer)
            return;

        // would love to do it outside of Update...
        var query = EntityQueryEnumerator<ArDisplaysComponent>();
        var ev = new ArDisplayUpdateEvent(viewer, _arOverlay.ActiveInterfaces);
        foreach (var entity in query)
        {
            RaiseLocalEvent(entity, ref ev);
        }
    }
}
