using System.Linq;
using Content.Client._Moffstation.Overlay.Overlays;
using Content.Client.Overlays;
using Content.Shared._Moffstation.ArInterface.Components;
using Content.Shared._Moffstation.Overlay.Components;
using Content.Shared._Moffstation.Overlay.Prototypes;
using Content.Shared.Inventory.Events;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._Moffstation.Overlay.Systems;

public sealed partial class ArInterfaceSystem : EquipmentHudSystem<ShowArInterfaceComponent>
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    private ArInterfaceOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new ArInterfaceOverlay();
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
}
