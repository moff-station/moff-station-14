using System.Numerics;
using Content.Shared._Moffstation.Explosion.Components;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Moffstation.Explosion;

public sealed partial class ExplosionShockWaveOverlay : Robust.Client.Graphics.Overlay
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private static readonly ProtoId<ShaderPrototype> ShockWaveShaderId = "ExplosionShockWave";

    /// <summary>
    ///     Screen-space radius a ring starts at; smaller rings distort the epicenter too much.
    /// </summary>
    private const float StartRadius = 0.13f;

    public const int MaxCount = 10;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly SharedTransformSystem _xformSystem;
    private readonly ShaderInstance _shader;

    private readonly Vector2[] _positions = new Vector2[MaxCount];
    private readonly float[] _times = new float[MaxCount];
    private readonly float[] _falloffPower = new float[MaxCount];
    private readonly float[] _sharpness = new float[MaxCount];
    private readonly float[] _width = new float[MaxCount];
    private int _count;

    public ExplosionShockWaveOverlay()
    {
        IoCManager.InjectDependencies(this);
        _xformSystem = _entMan.System<SharedTransformSystem>();
        _shader = _prototypeManager.Index(ShockWaveShaderId).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null || _cfg.GetCVar(CCVars.ReducedMotion))
            return false;

        _count = 0;

        foreach (var ent in _entMan.EntityQueryEnumerator<ExplosionShockWaveComponent, TransformComponent>())
        {
            if (ent.Comp2.MapID != args.MapId)
                continue;

            var tempCoords = args.Viewport.WorldToLocal(_xformSystem.GetWorldPosition(ent.Comp2));
            tempCoords.Y = 1 - (tempCoords.Y / args.Viewport.Size.Y);
            tempCoords.X /= args.Viewport.Size.X;

            _positions[_count] = tempCoords;
            _times[_count] = (float) (_timing.RealTime - ent.Comp1.StartTime).TotalSeconds + StartRadius;
            _falloffPower[_count] = ent.Comp1.FalloffPower;
            _sharpness[_count] = ent.Comp1.Sharpness;
            _width[_count] = ent.Comp1.Width;
            _count++;

            if (_count == MaxCount)
                break;
        }

        return _count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        _shader.SetParameter("count", _count);
        _shader.SetParameter("position", _positions);
        _shader.SetParameter("time", _times);
        _shader.SetParameter("falloffPower", _falloffPower);
        _shader.SetParameter("sharpness", _sharpness);
        _shader.SetParameter("width", _width);
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(args.WorldBounds, Color.White);
        worldHandle.UseShader(null);
    }
}
