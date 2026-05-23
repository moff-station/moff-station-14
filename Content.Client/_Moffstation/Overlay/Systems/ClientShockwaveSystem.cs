using Content.Client._Moffstation.Overlay.Overlays;
using Content.Shared._Moffstation.Overlay.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client._Moffstation.Overlay.Systems;

/// <summary>
/// This handles the ShockwaveComponent which interfaces with the ShockwaveOverlay
/// </summary>
public sealed class ClientShockwaveSystem : SharedShockwaveSystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private ShockwaveOverlay _overlay = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _overlay = new ShockwaveOverlay();
        _overlayMan.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay<ShockwaveOverlay>();
    }
}
