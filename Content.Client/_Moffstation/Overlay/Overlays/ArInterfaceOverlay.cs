using Content.Shared._Moffstation.ArInterface.Components;
using Content.Shared._Moffstation.Overlay.Prototypes;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Moffstation.Overlay.Overlays;

public sealed partial class ArInterfaceOverlay : Robust.Client.Graphics.Overlay
{
    [Dependency] private IEntityManager _entMan = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly List<ProtoId<ArInterfacePrototype>> _activeInterfaces = [];


    public ArInterfaceOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = -1; // place us underneath most shaders
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var sources = _entMan.EntityQueryEnumerator<ArDisplaysComponent>();
        foreach (var source in sources)
        {
            // var fetchEv = new GetArDisplaysEvent(in _activeInterfaces, null);
            // if display == null => continue
            // foreach var display in displays
            //     raise local event (display, DrawArDisplayEvent(in OverlayDrawArgs));
        }
    }
}
