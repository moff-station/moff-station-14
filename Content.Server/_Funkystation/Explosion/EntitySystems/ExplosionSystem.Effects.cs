using Content.Shared.Explosion;
using Robust.Shared.Prototypes;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class ExplosionSystem
{
    private void SpawnExplosionEffects(EntityUid uid, ProtoId<ExplosionPrototype> type)
    {
        if (!ProtoMan.Resolve(type, out var proto) || proto.Effects is not { } effects)
            return;

        foreach (var effect in effects.VisualEffects)
        {
            SpawnNextToOrDrop(effect, uid);
        }

        if (effects.MaxShrapnel <= 0)
            return;

        foreach (var effect in effects.ShrapnelEffects)
        {
            var shrapnelCount = _robustRandom.Next(effects.MinShrapnel, effects.MaxShrapnel);
            for (var i = 0; i < shrapnelCount; i++)
            {
                var angle = _robustRandom.NextAngle();
                var direction = angle.ToVec().Normalized() * 10;
                var shrapnel = SpawnNextToOrDrop(effect, uid);
                if (Exists(shrapnel))
                {
                    _throwingSystem.TryThrow(shrapnel, direction, effects.ShrapnelSpeed / 10);
                }
            }
        }
    }
}
