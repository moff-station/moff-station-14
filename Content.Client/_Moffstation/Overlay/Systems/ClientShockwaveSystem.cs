using Content.Shared._Moffstation.Overlay.Components;
using Content.Client._Moffstation.Overlay.Overlays;
using Content.Shared._Moffstation.Overlay.EntitySystems;
using Content.Shared._Moffstation.Overlay.Events;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Timing;

namespace Content.Client._Moffstation.Overlay.Systems;

/// <summary>
/// This handles the ShockwaveComponent which interfaces with the ShockwaveOverlay
/// </summary>
[UsedImplicitly]
public sealed class ClientShockwaveSystem : SharedShockwaveSystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _overlayMan.AddOverlay(new ShockwaveOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay<ShockwaveOverlay>();
    }
}
