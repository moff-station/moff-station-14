using Content.Server._Moffstation.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server._Moffstation.Objectives.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class DisruptionObjectiveSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisruptionObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<DisruptionObjectiveComponent> ent , ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress();
    }

    private float GetProgress()
    {
        return 0f;
    }
}
