using Content.Shared._ES.Objectives.Target.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Shared._ES.Objectives.Target;

/// <summary>
/// This handles <see cref="ESTargetPlayersObjectiveComponent"/>
/// </summary>
public sealed class ESTargetPlayersObjectiveSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTargetPlayersObjectiveComponent, ESGetObjectiveTargetCandidates>(OnGetObjectiveTargetCandidates);
    }

    private void OnGetObjectiveTargetCandidates(Entity<ESTargetPlayersObjectiveComponent> ent, ref ESGetObjectiveTargetCandidates args)
    {
        // HumanoidAppearanceComponent is used to prevent mice, pAIs, etc from being chosen
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, MobStateComponent, MindContainerComponent>();
        while (query.MoveNext(out var uid, out _, out var mobState, out var mindContainer))
        {
            // the player needs to have a mind and not be the excluded one +
            // the player has to be alive
            if (!_mind.TryGetMind(uid, out var mind, out var mindComp, mindContainer) ||
                mind == args.Holder.Owner ||
                !_mobState.IsAlive(uid, mobState))
                continue;

            args.Candidates.Add(uid);
        }
    }
}
