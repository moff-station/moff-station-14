using Content.Server._Moffstation.Objectives.Components;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.Objectives;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.Objectives.Systems;


public sealed class AntagRandomObjectivesSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagRandomObjectivesComponent, AfterAntagEntitySelectedEvent>(OnAntagSelected);
    }

    private void OnAntagSelected(Entity<AntagRandomObjectivesComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (!_mind.TryGetMind(args.Session, out var mindId, out var mind))
        {
            Log.Error($"Antag {ToPrettyString(args.EntityUid):player} was selected by {ToPrettyString(ent):rule} but had no mind attached!");
            return;
        }

        foreach (var set in ent.Comp.Sets)
        {
            if (!_random.Prob(set.Prob))
                continue;

            for (var pick = 0; pick < set.MaxPicks; pick++)
            {
                if (_objectives.GetRandomObjective(mindId, mind, set.Groups, float.PositiveInfinity) is not { } objective)
                    continue;

                ent.Comp.ObjectiveOptions.Add(objective);
            }
        }
    }
}
