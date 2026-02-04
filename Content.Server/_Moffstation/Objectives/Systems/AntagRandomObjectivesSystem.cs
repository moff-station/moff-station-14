using System.Linq;
using Content.Server._Moffstation.Objectives.Components;
using Content.Server.Antag;
using Content.Server.Objectives;
using Content.Shared._Moffstation.Objectives;
using Content.Shared.Mind;
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
        SubscribeAllEvent<ObjectivePickerSelected>(OnObjectivesSelected);
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

        if (!EnsureComp<PotentialObjectivesComponent>(mindId, out var potentialObjectives))
        {
            // Copying stuff over, probably a better way to do this but I am le tired
            potentialObjectives.MaxChoices = _random.Next(ent.Comp.MinChoices, ent.Comp.MaxChoices);
            potentialObjectives.MinChoices = ent.Comp.MinChoices;
            potentialObjectives.AutoSelectionDelay = ent.Comp.SelectionDelay;
        }

        foreach (var set in ent.Comp.Sets)
        {
            if (!_random.Prob(set.Prob))
                continue;

            foreach (var objective in _objectives.GetRandomObjectives(mindId, mind, set.Groups, float.MaxValue).Take(ent.Comp.MaxOptions))
            {
                if (_objectives.GetInfo(objective, mindId, mind) is not { } info)
                    continue;

                potentialObjectives.ObjectiveOptions.Add(GetNetEntity(objective), info);
            }
        }

        Dirty(mindId, potentialObjectives);
    }

    private void OnObjectivesSelected(ObjectivePickerSelected ev, EntitySessionEventArgs args)
    {
        var mindId = GetEntity(ev.MindId);

        if (!TryComp<MindComponent>(mindId, out var mindComp))
            return;

        if (!TryComp<PotentialObjectivesComponent>(mindId, out var potentialObjectivesComp))
            return;

        // Verify the objectives are actually in their component
        var objectiveIds = potentialObjectivesComp.ObjectiveOptions.Keys.ToHashSet();
        foreach (var objective in ev.SelectedObjectives)
        {
            if (objectiveIds.Contains(objective))
            {
                _mind.AddObjective(mindId, mindComp, GetEntity(objective));
            }
            else
            {
                TryQueueDel(GetEntity(objective));
            }
        }
        RemCompDeferred<PotentialObjectivesComponent>(mindId);
    }
}
