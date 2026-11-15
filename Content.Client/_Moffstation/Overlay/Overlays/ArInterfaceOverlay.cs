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

            var ev = new GetOverlayDrawingFunctionsEvent([]);
            foreach (var fun in ev.Functions)
            {
                fun(ent, args);
            }
        }
    }
}
