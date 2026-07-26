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
            if (_objectives.TryCreateObjective(ev.Mind, mind, spawn) is not { } objective)
                continue;

            _mindSystem.AddObjective(ev.Mind, mind, objective);
        }

        if (!ent.Comp.KeepOriginal)
            QueueDel(ent.Owner);
    }
}
