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

    public HashSet<ProtoId<ArInterfacePrototype>> ActiveInterfaces = [];


    public ArInterfaceOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = -1; // place us underneath most shaders
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        // ideally here a very simple loop like :
        // ev = new ArInterfaceDrawEvent(ActiveInterfaces, []);
        // Raise(ArInterfaceDrawEvent)
        // foreach(var toDraw in ev.Drawn)
        //      RaiseLocalEvent(draw, new ...)
        // with the idea that the drawing strategy of an entity is not defined here.
    }
}
