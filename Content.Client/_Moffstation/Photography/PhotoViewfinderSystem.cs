using Robust.Client.Graphics;

namespace Content.Client._Moffstation.Photography;

/// <summary>
/// Registers the photography overlays: the camera viewfinder, and the darkness over unseen tiles in photos.
/// </summary>
public sealed partial class PhotoViewfinderSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new PhotoViewfinderOverlay());
        _overlay.AddOverlay(new PhotoHiddenTilesOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<PhotoViewfinderOverlay>();
        _overlay.RemoveOverlay<PhotoHiddenTilesOverlay>();
    }
}
