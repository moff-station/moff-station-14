namespace Content.Shared.EntityTable.EntitySelectors;

/// <summary>
/// Gets spawns from all of <see cref="Children"/>.
/// </summary>
public sealed partial class AllSelector : EntityTableSelector
{
    /// <summary>
    /// All children selectors to pick from.
    /// </summary>
    [DataField(required: true)]
    public List<EntityTableSelector> Children;

    // Moffstation - Begin - Rewrite table selectors with visitors. Early merge of https://github.com/Space-Wizards-Federation/space-station-14/pull/177
    public override TResult Accept<TContext, TResult>(IEntityTableVisitor<TContext, TResult> visitor, TContext args) =>
        visitor.VisitAllSelector(this, args);

    // protected override IEnumerable<EntProtoId> GetSpawnsImplementation(IRobustRandom rand,
    //     IEntityManager entMan,
    //     IPrototypeManager proto,
    //     EntityTableContext ctx)
    // {
    //     foreach (var child in Children)
    //     {
    //         foreach (var spawn in child.GetSpawns(rand, entMan, proto, ctx))
    //         {
    //             yield return spawn;
    //         }
    //     }
    // }
    //
    // protected override IEnumerable<(EntProtoId spawn, double)> ListSpawnsImplementation(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    // {
    //     foreach (var child in Children)
    //     {
    //         foreach (var (spawn, prob) in child.ListSpawns(entMan, proto, ctx))
    //         {
    //             yield return (spawn, prob);
    //         }
    //     }
    // }
    //
    // protected override IEnumerable<(EntProtoId spawn, double)> AverageSpawnsImplementation(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    // {
    //     foreach (var child in Children)
    //     {
    //         foreach (var (spawn, prob) in child.AverageSpawns(entMan, proto, ctx))
    //         {
    //             yield return (spawn, prob);
    //         }
    //     }
    // }
    // Moffstation - End
}
