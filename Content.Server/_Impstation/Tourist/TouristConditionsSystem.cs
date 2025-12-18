using Content.Server._Impstation.Tourist.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Paper;
using Robust.Shared.Containers;

namespace Content.Server._Impstation.Tourist;

/// <summary>
/// Handles tourist objective conditions
/// </summary>
public sealed class TouristConditionsSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    private EntityQuery<ContainerManagerComponent> _containerQuery;
    public override void Initialize()
    {
        _containerQuery = GetEntityQuery<ContainerManagerComponent>();

        SubscribeLocalEvent<TouristPhotosConditionComponent, ObjectiveGetProgressEvent>(OnPhotosGetProgress);
        SubscribeLocalEvent<StampedPapersConditionComponent, ObjectiveGetProgressEvent>(OnStampedPapersGetProgress);
    }

    // multiple photographs of something

    private void OnPhotosGetProgress(EntityUid uid, TouristPhotosConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = PhotoProgress(comp, _number.GetTarget(uid));
    }

    private float PhotoProgress(TouristPhotosConditionComponent comp, int target)
    {
        // no dividing by zero!!
        if (target == 0)
            return 1f;

        return MathF.Min(comp.Photos / (float)target, 1f);
    }

    // stamp collection!
    private void OnStampedPapersGetProgress(Entity<StampedPapersConditionComponent> entity, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = StampedPapersProgress((args.MindId, args.Mind), entity.Comp, _number.GetTarget(entity));
    }
    private float StampedPapersProgress(Entity<MindComponent> mind, StampedPapersConditionComponent comp, float target)
    {
        if (!_containerQuery.TryGetComponent(mind.Comp.OwnedEntity, out var currentManager))
            return 0;

        var containerStack = new Stack<ContainerManagerComponent>();
        var foundStamps = new List<StampDisplayInfo>();

        // Recursively check each container for the item
        // Checks inventory, bag, implants, etc.
        do
        {
            foreach (var container in currentManager.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    if (TryComp<PaperComponent>(entity, out var paper))
                    {
                        foreach (var stamp in paper.StampedBy)
                        {
                            if (!foundStamps.Contains(stamp))
                                foundStamps.Add(stamp);
                        }
                    }

                    // If it is a container check its contents
                    if (_containerQuery.TryGetComponent(entity, out var containerManager))
                        containerStack.Push(containerManager);
                }
            }
        } while (containerStack.TryPop(out currentManager));

        var result = foundStamps.Count / target;
        result = Math.Clamp(result, 0, 1);
        return result;
    }
}
