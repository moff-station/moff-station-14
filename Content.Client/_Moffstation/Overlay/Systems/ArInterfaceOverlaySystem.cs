using System.Linq;
using Content.Client._Moffstation.Overlay.Overlays;
using Content.Client.Overlays;
using Content.Shared._Moffstation.ArInterface.Components;
using Content.Shared._Moffstation.Overlay.Components;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client._Moffstation.Overlay.Systems;

public sealed partial class ArInterfaceOverlaySystem : EquipmentHudSystem<ShowArInterfaceComponent>
{
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private IPlayerManager _playerMan = default!;

    private ArInterfaceOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new ArInterfaceOverlay();
    }

    public override void Update(float dt)
    {
        if (! IsActive || _playerMan.LocalEntity is not {} viewer)
            return;

        // would love to do it outside of Update...
        var query = EntityQueryEnumerator<ArDisplaysComponent>();
        var ev = new ArDisplayUpdateEvent(viewer, _overlay.ActiveInterfaces);
        foreach (var entity in query)
        {
            RaiseLocalEvent(entity, ref ev);
        }
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ShowArInterfaceComponent> args)
    {
        base.UpdateInternal(args);
        _overlay.ActiveInterfaces = args.Components.Select(x => x.Interface).ToHashSet();
        _overlayMan.AddOverlay(_overlay);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();
        _overlayMan.RemoveOverlay(_overlay);
    }

    [SubscribeLocalEvent]
    private void OnAfterHandleState(Entity<ShowArInterfaceComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshOverlay();
    }
}
