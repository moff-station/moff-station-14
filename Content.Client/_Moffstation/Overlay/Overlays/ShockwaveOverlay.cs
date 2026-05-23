using System.Linq;
using System.Numerics;
using Content.Shared._Moffstation.Overlay.Components;
using Content.Shared._Moffstation.Overlay.EntitySystems;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Moffstation.Overlay.Overlays;

public sealed class ShockwaveOverlay : Robust.Client.Graphics.Overlay
{
    private static readonly ProtoId<ShaderPrototype> ShockwaveShader = "ShockwaveShader";

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;

    private readonly SharedTransformSystem _transformSystem;
    private readonly SharedShockwaveSystem _shockwaveSystem;

    private readonly ShaderInstance _shader;
    private readonly int _instanceLimit = 10;
    private int _instanceCount;
    private float _distortionScale = 1.0f;

    private IEnumerable<Vector2> _epicenters = [];
    private IEnumerable<float> _intensities = [];
    private IEnumerable<float> _ranges = [];
    private IEnumerable<float> _fallOffs = [];
    private IEnumerable<float> _timeScales = [];

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public ShockwaveOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(ShockwaveShader).InstanceUnique();
        _transformSystem = _entityManager.System<SharedTransformSystem>();
        _shockwaveSystem = _entityManager.System<SharedShockwaveSystem>();
        _configManager.OnValueChanged(CCVars.ReducedMotion, OnReducedMotionChanged, invokeImmediately: true);
    }

    private void OnReducedMotionChanged(bool reducedMotion)
    {
        _distortionScale = reducedMotion ? 0.01f : 1f;
    }

    /// <inheritdoc cref="Overlay.FrameUpdate"/>
    protected override void FrameUpdate(FrameEventArgs args)
    {
        _shockwaveSystem.UpdateComponents();
    }

    /// <inheritdoc cref="Overlay.BeforeDraw"/>
    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return false;

        if (!_entityManager.TryGetComponent(_playerManager.LocalSession?.AttachedEntity, out EyeComponent? eyeComp) ||
            args.Viewport.Eye != eyeComp.Eye ||
            args.ViewportControl == null)
            return false;

        _instanceCount = 0;
        foreach (var shockwaveEntity in _entityManager.EntityQuery<Shared._Moffstation.Overlay.Components.ShockwaveComponent, TransformComponent>())
        {
            if (!shockwaveEntity.Item1.Active)
                continue;

            _epicenters = _epicenters.Append(
                args.ViewportControl.WorldToScreen(
                    _transformSystem.GetWorldPosition(shockwaveEntity.Item2)));
            _intensities = _intensities.Append(shockwaveEntity.Item1.Intensity * _distortionScale);
            _ranges = _ranges.Append(shockwaveEntity.Item1.Range * _distortionScale);
            _fallOffs = _fallOffs.Append(shockwaveEntity.Item1.FallOff);
            _timeScales = _timeScales.Append(shockwaveEntity.Item1.TimeScale);

            if (++_instanceCount >= _instanceLimit)
                break;
        }

        return true;
    }

    /// <inheritdoc cref="Overlay.Draw"/>
    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        if (_instanceCount == 0)
            return;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("EPICENTERS", _epicenters.ToArray());
        _shader.SetParameter("INTENSITIES", _intensities.ToArray());
        _shader.SetParameter("RANGES", _ranges.ToArray());
        _shader.SetParameter("FALLOFFS", _fallOffs.ToArray());
        _shader.SetParameter("TIMESCALES", _timeScales.ToArray());
        _shader.SetParameter("COUNT", _instanceCount);

        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}
