using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Starlight.Overlays;

public sealed partial class NightVisionOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "ModernNightVisionShader";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _nightVisionShader;


    public Color TintColor;
    public float TintIntensity;


    public NightVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _nightVisionShader = _prototypeManager.Index(Shader).InstanceUnique();
        ZIndex = 10000;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _nightVisionShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _nightVisionShader.SetParameter("TintColor", TintColor);
        _nightVisionShader.SetParameter("TintIntensity", TintIntensity);
        handle.UseShader(_nightVisionShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
