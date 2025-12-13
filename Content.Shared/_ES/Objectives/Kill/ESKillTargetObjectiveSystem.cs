using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Objectives.Kill.Components;
using Content.Shared._ES.Objectives.Target;
using Content.Shared._ES.Objectives.Target.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Shared._ES.Objectives.Kill;

public sealed class ESKillTargetObjectiveSystem : ESBaseObjectiveSystem<ESKillTargetObjectiveComponent>
{
    [Dependency] private readonly ESTargetObjectiveSystem _targetObjective = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    // TODO: this is like plainly lazy and unperformant but I need this code done.
    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        var query = EntityQueryEnumerator<ESKillTargetObjectiveComponent, ESTargetObjectiveComponent, ESObjectiveComponent>();
        while (query.MoveNext(out var uid, out _, out var target, out var comp))
        {
            if (!_targetObjective.TryGetTarget((uid, target), out var objTarget) ||
                objTarget != ev.Target)
                continue;

            ObjectivesSys.RefreshObjectiveProgress((uid, comp));
        }
    }

    protected override void GetObjectiveProgress(Entity<ESKillTargetObjectiveComponent> ent, ref ESGetObjectiveProgressEvent args)
    {
        if (!_targetObjective.TryGetTarget(ent.Owner, out var target))
        {
            args.Progress = 1;
            return;
        }

        if (!TryComp<MobStateComponent>(target.Value, out var mobState))
            return;

        args.Progress = mobState.CurrentState switch
        {
            MobState.Alive => 0f,
            MobState.Critical => 0.5f,
            MobState.Dead => 1,
            _ => 1,
        };
    }
}
