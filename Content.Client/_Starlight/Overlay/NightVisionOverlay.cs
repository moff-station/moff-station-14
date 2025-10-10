namespace Content.Client._Starlight.Overlay;

/// This overlay gives the screen a blue tint.
public sealed class NightVisionOverlay()
    : BaseVisionOverlay("ModernNightVisionShader", VisionOverlayZIndex.NightVision);
