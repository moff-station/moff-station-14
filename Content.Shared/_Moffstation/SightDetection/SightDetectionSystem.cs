using Content.Shared.Interaction;
using Content.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.SightDetection;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class SightDetectionSystem : EntitySystem
{
    [Dependency] private IGameTiming _time = default!;
    [Dependency] private SharedInteractionSystem _interactions = default!;

    /// <inheritdoc/>
    public override void Update(float delta)
    {
        var query = EntityQueryEnumerator<SightDetectorComponent>();
        foreach (var ent in query)
        {
            if (_time.CurTime < ent.Comp.NextUpdate)
                continue;


            ent.Comp.NextUpdate = _time.CurTime + ent.Comp.UpdateCooldown;
        }
    }

    private void UpdateDetector(Entity<SightDetectorComponent> ent)
    {
        // todo : acquire new targets.


        HashSet<EntityUid> toRemove = [];

        foreach (var sighted in ent.Comp.Sights)
        {
            if (!_interactions.InRangeUnobstructed(
                    ent.Owner,
                    sighted,
                    ent.Comp.Range,
                    ent.Comp.IgnoreOpacity ? CollisionGroup.None : CollisionGroup.Opaque))
            {
                toRemove.Add(sighted);
            }
        }

        foreach (var removed in toRemove)
        {
            var ev = new DetectorUnseeTargetEvent(removed;
            RaiseLocalEvent(ent, ref ev);
            ent.Comp.Sights.Remove(removed);
        }
    }
}
