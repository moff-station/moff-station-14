using Content.Server._Moffstation.Power.Components;
using Content.Server.Light.EntitySystems;
using Content.Shared.Light.Components;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.Power.EntitySystems;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class LightExplodeTimerSystem : EntitySystem
{
    [Dependency] private PoweredLightSystem _poweredLight = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LightExplodeTimerComponent, PoweredLightComponent, TransformComponent>();
        while (query.MoveNext(out var lightUid, out var explodeTimer, out var light, out var xform))
        {
            explodeTimer.ExplodeTimer -= frameTime;
            if (explodeTimer.ExplodeTimer > 0.0f)
                continue;

            _poweredLight.TryDestroyBulb(lightUid);

            if (!_random.Prob(explodeTimer.SparksProbability))
                Spawn(explodeTimer.SparksPrototype, xform.Coordinates);

            RemComp(lightUid, explodeTimer);
        }
    }
}
