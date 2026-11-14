using Content.Client._Moffstation.ArInterface;
using Content.Client._Moffstation.Overlay.Events;
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
        ZIndex = -1; // to be underneath eventual shaders.
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var entities = _entMan.EntityQueryEnumerator<ArElementComponent>();

        foreach (var ent in entities)
        {
            if (!ActiveInterfaces.Contains(ent.Comp.Interface))
                continue;

            // i would love to make this a "ArDrawEvent that handle the whole drawing call
            // but sadly OverlayDrawArgs being a readonly ref struct does not allow it to be passed around in events
            var ev = new GetOverlayDrawingFunctionsEvent([]);
            foreach (var fun in ev.Functions)
            {
                fun(ent, args);
            }
        }
    }
}
