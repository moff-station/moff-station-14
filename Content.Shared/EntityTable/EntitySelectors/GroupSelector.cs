using System.Linq;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.EntitySelectors;

/// <summary>
/// Gets the spawns from one of the child selectors, based on the weight of the children
/// </summary>
public sealed partial class GroupSelector : EntityTableSelector
{
    [DataField(required: true)]
    public List<EntityTableSelector> Children = new();

    protected override IEnumerable<EntProtoId> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx,
        Dictionary<EntityTableSelector, float>? localPool) // Moffstation - WithReplacement Selector
    {
        // Moffstation - Start - WithReplacement Selector
        var children = localPool ?? new Dictionary<EntityTableSelector, float>(Children.Count);
        var populated = children.Count > 0;
        // Moffstation - End

        if (!populated) // Moffstation - WithReplacement Selector
        {
            foreach (var child in Children)
            {
                // Don't include invalid groups
                if (!child.CheckConditions(entMan, proto, ctx))
                    continue;

                children.Add(child, child.Weight);
            }
        }

        if (children.Count == 0)
            return Array.Empty<EntProtoId>();

        var pick = SharedRandomExtensions.Pick(children, rand);

        // Moffstation - Start - WithReplacement Selector
        if (!WithReplacement || localPool != null)
            children.Remove(pick);
        // Moffstation - End

        return pick.GetSpawns(rand, entMan, proto, ctx);
    }

    protected override IEnumerable<(EntProtoId spawn, double)> ListSpawnsImplementation(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        var totalWeight = Children.Sum(x => x.Weight);

        foreach (var child in Children)
        {
            var weightMod = child.Weight / totalWeight;
            foreach (var (ent, prob) in child.ListSpawns(entMan, proto, ctx, weightMod))
            {
                yield return (ent, prob);
            }
        }
    }

    protected override IEnumerable<(EntProtoId spawn, double)> AverageSpawnsImplementation(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        var totalWeight = Children.Sum(x => x.Weight);

        foreach (var child in Children)
        {
            var weightMod = child.Weight / totalWeight;
            foreach (var (ent, prob) in child.AverageSpawns(entMan, proto, ctx, weightMod))
            {
                yield return (ent, prob);
            }
        }
    }
}
