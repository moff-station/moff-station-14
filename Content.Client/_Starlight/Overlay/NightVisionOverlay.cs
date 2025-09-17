using Robust.Client.Graphics;

namespace Content.Client._Starlight.Overlay;

/// <summary>
/// This overlay gives the screen a blue tint.
/// </summary>
/// <param name="shader"></param>
public sealed class NightVisionOverlay(ShaderPrototype shader)
    : BaseVisionOverlay(shader, VisionOverlayZIndex.NightVision);
