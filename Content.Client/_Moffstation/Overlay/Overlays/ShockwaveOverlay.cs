using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
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
using Robust.Shared.Toolshed.Commands.Generic;

namespace Content.Client._Moffstation.Overlay.Overlays;

public sealed partial class ShockwaveOverlay : Robust.Client.Graphics.Overlay
{
    private static readonly ProtoId<ShaderPrototype> ShockwaveShader = "ShockwaveShader";

    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly SharedTransformSystem _transformSystem;

    private readonly ShaderInstance _shader;
    private const int InstanceLimit = 10;
    private float _distortionScale = 1.0f;

    private ShaderArgs _shaderArgs;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public ShockwaveOverlay()
    {
        ZIndex = 8;

        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(ShockwaveShader).InstanceUnique();
        _transformSystem = _entityManager.System<SharedTransformSystem>();
        _configManager.OnValueChanged(CCVars.ReducedMotion, OnReducedMotionChanged, invokeImmediately: true);

        _shaderArgs = new ShaderArgs(InstanceLimit);
    }

    private void OnReducedMotionChanged(bool reducedMotion)
    {
        _distortionScale = reducedMotion ? 0.01f : 1f;
    }

    /// <inheritdoc cref="Overlay.BeforeDraw"/>
    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return false;

        _shaderArgs.Clear();

        var enumerator = _entityManager.AllEntityQueryEnumerator<ShockwaveComponent, TransformComponent>();
        while (enumerator.MoveNext(out var shockwaveComp, out var transformComp))
        {
            if (transformComp.MapID != args.MapId)
                continue;

            var screenCoords = args.Viewport.WorldToLocal(_transformSystem.GetWorldPosition(transformComp));
            screenCoords.X = screenCoords.X/args.Viewport.Size.X;
            screenCoords.Y = 1 - screenCoords.Y/args.Viewport.Size.Y;

            _shaderArgs.Append(
                screenCoords,
                shockwaveComp.Intensity,
                shockwaveComp.Width * _distortionScale,
                shockwaveComp.FallOff,
                shockwaveComp.PowerFactor * _distortionScale,
                (float)shockwaveComp.StartTime.TotalSeconds,
                shockwaveComp.TimeScale
            );

            if (_shaderArgs.Count >= InstanceLimit)
                break;
        }

        return true;
    }

    /// <inheritdoc cref="Overlay.Draw"/>
    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || args.Viewport.Eye == null)
            return;

        if (_shaderArgs.Count == 0)
            return;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("RENDER_SCALE", args.Viewport.Eye.Scale);
        _shader.SetParameter("CURR_TIME", (float) _timing.CurTime.TotalSeconds);

        _shaderArgs.SetShaderParams(_shader);

        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }

    private struct ShaderArgs
    {
        private readonly List<Vector2> _epicenters;
        private readonly List<float> _intensities;
        private readonly List<float> _widths;
        private readonly List<float> _fallOffs;
        private readonly List<float> _powerFactors;
        private readonly List<float> _startTimes;
        private readonly List<float> _timeScales ;

        public int Count;

        public ShaderArgs(int instanceLimit)
        {
            _epicenters = new List<Vector2>(instanceLimit);
            _intensities = new List<float>(instanceLimit);
            _widths = new List<float>(instanceLimit);
            _fallOffs = new List<float>(instanceLimit);
            _powerFactors = new List<float>(instanceLimit);
            _startTimes = new List<float>(instanceLimit);
            _timeScales = new List<float>(instanceLimit);
        }

        public void Clear()
        {
            _epicenters.Clear();
            _intensities.Clear();
            _widths.Clear();
            _fallOffs.Clear();
            _powerFactors.Clear();
            _startTimes.Clear();
            _timeScales.Clear();

            Count = 0;
        }

        public void Append(
            Vector2 epicenter,
            float intensity,
            float width,
            float fallOff,
            float powerFactor,
            float startTime,
            float timeScale
            )
        {
            _epicenters.Add(epicenter);
            _intensities.Add(intensity);
            _widths.Add(width);
            _fallOffs.Add(fallOff);
            _powerFactors.Add(powerFactor);
            _startTimes.Add(startTime);
            _timeScales.Add(timeScale);
            Count++;
        }

        public void SetShaderParams(ShaderInstance shader)
        {
            shader.SetParameter("EPICENTERS", _epicenters.ToArray());
            shader.SetParameter("INTENSITIES", _intensities.ToArray());
            shader.SetParameter("WIDTHS", _widths.ToArray());
            shader.SetParameter("FALLOFFS", _fallOffs.ToArray());
            shader.SetParameter("START_TIMES", _startTimes.ToArray());
            shader.SetParameter("POWER_FACTORS", _powerFactors.ToArray());
            shader.SetParameter("TIME_SCALES", _timeScales.ToArray());
            shader.SetParameter("COUNT", Count);
        }
    }
}
