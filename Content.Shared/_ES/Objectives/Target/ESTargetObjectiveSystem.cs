using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Objectives.Target.Components;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Random;

namespace Content.Shared._ES.Objectives.Target;

public sealed class ESTargetObjectiveSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ESSharedObjectiveSystem _objective = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTargetObjectiveComponent, ESInitializeObjectiveEvent>(OnInitializeObjective);
        SubscribeLocalEvent<ESObjectiveTargetComponent, ComponentShutdown>(OnTargetShutdown);
    }

    private void OnInitializeObjective(Entity<ESTargetObjectiveComponent> ent, ref ESInitializeObjectiveEvent args)
    {
        if (!TryGetCandidate(args.Holder, ent, out var candidate))
            return;

        // TODO: pull out into a SetTarget method, probably.
        ent.Comp.Target = candidate;

        if (ent.Comp.Title != null)
        {
            if (!_mind.TryGetMind(ent.Comp.Target.Value, out var mind, out var mindComp) ||
                mindComp.CharacterName is not { } name)
                return;
            _job.MindTryGetJobName(mind, out var job);

            var title = Loc.GetString(ent.Comp.Title, ("targetName", name), ("job", job));
            _metaData.SetEntityName(ent, title);
        }

        var comp = EnsureComp<ESObjectiveTargetComponent>(ent.Comp.Target.Value);
        comp.Objectives.Add(ent);

        // TODO: raise event on target selected. for additional setup
    }

    private void OnTargetShutdown(Entity<ESObjectiveTargetComponent> ent, ref ComponentShutdown args)
    {
        foreach (var objective in ent.Comp.Objectives)
        {
            if (TryComp<ESTargetObjectiveComponent>(objective, out var comp))
                comp.Target = null;
        }
    }

    public bool TryGetCandidate(
        Entity<ESObjectiveHolderComponent> holder,
        Entity<ESTargetObjectiveComponent> ent,
        [NotNullWhen(true)] out EntityUid? candidate)
    {
        candidate = null;
        var candidates = GetTargetCandidates(holder, ent).ToList();
        if (candidates.Count == 0)
            return false;

        candidate = _random.Pick(candidates);
        return true;
    }

    public IEnumerable<EntityUid> GetTargetCandidates(Entity<ESObjectiveHolderComponent> holder, Entity<ESTargetObjectiveComponent> ent)
    {
        var otherTargets = new HashSet<EntityUid>();
        foreach (var objective in _objective.GetObjectives<ESTargetObjectiveComponent>(holder.AsNullable()))
        {
            if (objective.Comp.Target is { } target)
                otherTargets.Add(target);
        }

        var ev = new ESGetObjectiveTargetCandidates(holder, []);
        RaiseLocalEvent(ent, ref ev);

        foreach (var candidate in ev.Candidates)
        {
            // Don't share targets between multiple objectives
            // This technically isn't necessary for ALL targeted objectives,
            // but i think for gameplay purposes there really isnt a reason to allow it.
            if (otherTargets.Contains(candidate))
                continue;

            var checkEv = new ESValidateObjectiveTargetCandidates(holder, candidate);
            RaiseLocalEvent(ent, ref checkEv);

            if (checkEv.Valid)
                yield return candidate;
        }
    }

    public bool TryGetTarget(Entity<ESTargetObjectiveComponent?> ent, [NotNullWhen(true)] out EntityUid? candidate)
    {
        candidate = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        candidate = ent.Comp.Target;
        return candidate != null;
    }
}
