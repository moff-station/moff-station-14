using System.Linq;
using Content.Client._Funkystation.Clothing.Systems;
using Content.Client.Overlays;
using Content.Shared._Moffstation.Overlay.Components;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._Moffstation.Overlay.Systems;

public sealed partial class ShowShaderSystem : EquipmentHudSystem<ShowShaderComponent>
{
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;

    private GoggleShaderOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new GoggleShaderOverlay(_protoMan);
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ShowShaderComponent> args)
    {
        base.UpdateInternal(args);

        _overlay.ActiveShaders.Clear();
        _overlay.ActiveShaders = args.Components.Select(x => (x.Shader, x.Color)).ToList();
        _overlayMan.AddOverlay(_overlay);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();
        _overlayMan.RemoveOverlay(_overlay);
    }
}
