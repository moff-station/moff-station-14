using System.Linq;
using Content.Server._Moffstation.Objectives.Components;
using Content.Server.Antag;
using Content.Server.Objectives;
using Content.Shared._Moffstation.Objectives;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.Objectives.Systems;


public sealed class AntagRandomObjectivesSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagRandomObjectivesComponent, AfterAntagEntitySelectedEvent>(OnAntagSelected);
    }

    private void OnAntagSelected(Entity<AntagRandomObjectivesComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (args.Session == null)
            return;

        if (!_mind.TryGetMind(args.Session, out var mindId, out var mind))
        {
            Log.Error($"Antag {ToPrettyString(args.EntityUid):player} was selected by {ToPrettyString(ent):rule} but had no mind attached!");
            return;
        }

        var potentialObjectives = EnsureComp<PotentialObjectivesComponent>(mindId);

        foreach (var set in ent.Comp.Sets)
        {
            if (!_random.Prob(set.Prob))
                continue;

            foreach (var objective in _objectives.GetRandomObjectives(mindId, mind, set.Groups, float.MaxValue).Take(ent.Comp.MaxOptions))
            {
                if (_objectives.GetInfo(objective, mindId, mind) is not { } info)
                    continue;

                potentialObjectives.ObjectiveOptions.Add((GetNetEntity(objective), info));
            }
        }

        Dirty(mindId, potentialObjectives);
    }
}
