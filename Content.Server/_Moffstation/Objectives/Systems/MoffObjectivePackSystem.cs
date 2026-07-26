using Content.Server._Moffstation.Objectives.Components;
using Content.Shared._Moffstation.Objectives;
using Content.Shared.EntityTable;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Map;

namespace Content.Server._Moffstation.Objectives.Systems;


public sealed partial class MoffObjectivePackSystem : EntitySystem
{
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private SharedObjectivesSystem _objectives = default!;
    [Dependency] private SharedMindSystem _mindSystem = default!;

    [Dependency] private EntityQuery<MindComponent> _mindQuery;

    [SubscribeLocalEvent]
    public void OnObjectiveAdded(Entity<MoffObjectivePackComponent> ent, ref ObjectiveAddedEvent ev)
    {
        if (!_mindQuery.TryComp(ev.Mind, out var mind))
            return;

        var spawns = _entityTable.GetSpawns(ent.Comp.Objectives);

        foreach (var spawn in spawns)
        {
            _mindSystem.TryAddObjective(ev.Mind, mind, spawn);
        }

        if (!ent.Comp.KeepOriginal)
            _mindSystem.TryRemoveObjective(ev.Mind, mind, mind.Objectives.IndexOf(ent.Owner));
    }
}
