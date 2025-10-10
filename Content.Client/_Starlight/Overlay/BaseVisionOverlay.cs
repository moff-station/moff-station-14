using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Starlight.Overlay;

/// <summary>
/// This abstract contains all the behavior needed to apply a "vision" overlay to a client's view. This means it passes
/// the entire screen view texture through a shader, and that's it.
///
/// Note that while this could be used as a simple class which is instantiated with different values to produce
/// different effects, RobustToolbox assumes all overlays are distinct C# types. Therefore, it is necessary for each
/// distinct overlay to be a distinct type. (This remark is included because the original Starlight source had a "mini
/// rant" about this).
/// </summary>
public abstract class BaseVisionOverlay : Robust.Client.Graphics.Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly ShaderInstance _shader;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected BaseVisionOverlay(ProtoId<ShaderPrototype> shaderId, VisionOverlayZIndex zIndex)
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(shaderId).InstanceUnique();
        ZIndex = (int)zIndex;
    }

    /// <inheritdoc cref="Overlay.BeforeDraw"/>
    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalSession?.AttachedEntity, out EyeComponent? eyeComp) ||
            args.Viewport.Eye != eyeComp.Eye)
            return false;

        return _playerManager.LocalSession?.AttachedEntity != null;
    }

    /// <inheritdoc cref="Overlay.Draw"/>
    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}

/// <summary>
/// Z-Indexes of vision overlays. Values don't really matter, they just need to be unique.
/// </summary>
public enum VisionOverlayZIndex
{
    NightVision = 10000,
}
