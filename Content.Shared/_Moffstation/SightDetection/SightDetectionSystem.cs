using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.SightDetection;

public sealed partial class SightDetectionSystem : EntitySystem
{
    [Dependency] private IGameTiming _time = default!;
    [Dependency] private SharedInteractionSystem _interactions = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    /// <inheritdoc/>
    public override void Update(float delta)
    {
        var detectors = EntityQueryEnumerator<SightDetectorComponent>();
        var targets = EntityQueryEnumerator<SightDetectorTargetComponent>();
        foreach (var ent in detectors)
        {
            if (_time.CurTime < ent.Comp.NextUpdate)
                continue;

            UpdateDetector(ent, targets);

            ent.Comp.NextUpdate = _time.CurTime + ent.Comp.UpdateCooldown;
        }
    }

    private void UpdateDetector(Entity<SightDetectorComponent> ent, EntityQueryEnumerator<SightDetectorTargetComponent> targets)
    {
        HashSet<EntityUid> toAdd = [];

        foreach (var target in targets)
        {
            if (target.Comp.Key != ent.Comp.Key ||
                _xform.GetMapId(target.Owner) != _xform.GetMapId(ent.Owner) ||
                ent.Comp.Sights.Contains(target))
                continue;

            /*
            if (_interactions.InRangeUnobstructed(
                    ent.Owner,
                    target.Owner,
                    ent.Comp.Range,
                    ent.Comp.IgnoreOpacity ? CollisionGroup.None : CollisionGroup.Opaque))
                toAdd.Add(target.Owner);
            */
            if ((_xform.GetWorldPosition(target) - _xform.GetWorldPosition(ent.Owner)).Length() <= ent.Comp.Range)
                toAdd.Add(ent.Owner);
        }


        HashSet<EntityUid> toRemove = [];

        foreach (var sighted in ent.Comp.Sights)
        {
            /*
            if (!_interactions.InRangeUnobstructed(
                    ent.Owner,
                    sighted,
                    ent.Comp.Range,
                    ent.Comp.IgnoreOpacity ? CollisionGroup.None : CollisionGroup.Opaque))
            {
                toRemove.Add(sighted);
            }
            */
            if ((_xform.GetWorldPosition(sighted) - _xform.GetWorldPosition(ent.Owner)).Length() <= ent.Comp.Range)
                toRemove.Add(ent.Owner);
        }

        foreach (var removed in toRemove)
        {
            var ev = new DetectorUnseeTargetEvent(removed);
            RaiseLocalEvent(ent, ref ev);
            ent.Comp.Sights.Remove(removed);
        }

        foreach (var added in toAdd)
        {
            var ev = new DetectorSeeTargetEvent(added);
            RaiseLocalEvent(ent, ref ev);
            ent.Comp.Sights.Add(added);
        }
    }
}
