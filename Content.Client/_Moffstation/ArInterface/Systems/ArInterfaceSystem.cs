using Content.Client._Moffstation.Overlay.Overlays;
using Robust.Client.Graphics;

namespace Content.Client._Moffstation.ArInterface.Systems;

public sealed partial class ArInterfaceSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;
    private ArInterfaceOverlay _overlay = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        _overlay = new ArInterfaceOverlay();
    }
}
