using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Utility;

namespace Content.Client._Funkystation.Clothing;

public sealed class WeldingMaskOverlay(IResourceCache cache) : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public ResPath? CurrentTexturePath;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (CurrentTexturePath is not {} texturePath)
            return;

        var texture = cache.GetTexture(texturePath);
        var handle = args.WorldHandle;

        handle.DrawTextureRect(texture, args.WorldBounds, Color.White);
    }
}
