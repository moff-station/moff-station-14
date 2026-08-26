using Robust.Shared.Player;

// Extension of an upstream file
// ReSharper disable once CheckNamespace
namespace Content.Server.GameTicking.Rules;


public sealed partial class RuleGridsSystem
{
    [Dependency] private EntityQuery<ActorComponent> _actorQuery;

    public void DeleteRuleGrids(Entity<RuleGridsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        foreach (var grid in ent.Comp.MapGrids)
        {
            if (TerminatingOrDeleted(grid))
                continue;

            EvacuatePlayers(grid);
            QueueDel(grid);
        }

        ent.Comp.MapGrids.Clear();
        ent.Comp.Map = null;
    }

    private void EvacuatePlayers(EntityUid grid)
    {
        if (_transform.GetMap(grid) is not { } map)
            return;

        using var children = Transform(grid).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (!_actorQuery.HasComp(child))
                continue;

            _transform.SetParent(child, map);
        }
    }
}
