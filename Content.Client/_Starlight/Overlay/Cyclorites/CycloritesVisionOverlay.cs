using Robust.Client.Graphics;
using Robust.Client.Player;
using Content.Shared.CCVar;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.Eye.Blinding.Components;
using Robust.Shared.Configuration;

namespace Content.Client._Starlight.Overlay.Cyclorites;

public sealed class CycloritesVisionOverlay : Robust.Client.Graphics.Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _cycloriteShader;
    public CycloritesVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _cycloriteShader = _prototypeManager.Index<ShaderPrototype>("CycloriteShader").InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalSession?.AttachedEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return false;

        if (!_entityManager.TryGetComponent<CycloritesVisionComponent>(playerEntity, out var blurComp))
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;

        _cycloriteShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        worldHandle.UseShader(_cycloriteShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}
