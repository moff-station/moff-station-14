using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.EntitySelectors;

/// <summary>
/// A table which simply delegates to the table identified by <see cref="TableId"/>.
/// Can be used to reuse common tables.
/// </summary>
public sealed partial class NestedSelector : EntityTableSelector
{
    [DataField(required: true)]
    public ProtoId<EntityTablePrototype> TableId;

    // Moffstation - Begin - Rewrite table selectors with visitors. Early merge of https://github.com/Space-Wizards-Federation/space-station-14/pull/177
    public override TResult Accept<TContext, TResult>(IEntityTableVisitor<TContext, TResult> visitor, TContext args) =>
        visitor.VisitNestedSelector(this, args);

    // protected override IEnumerable<EntProtoId> GetSpawnsImplementation(IRobustRandom rand,
    //     IEntityManager entMan,
    //     IPrototypeManager proto,
    //     EntityTableContext ctx)
    // {
    //     return proto.Index(TableId).Table.GetSpawns(rand, entMan, proto, ctx);
    // }
    //
    // protected override IEnumerable<(EntProtoId spawn, double)> ListSpawnsImplementation(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    // {
    //     return proto.Index(TableId).Table.ListSpawns(entMan, proto, ctx);
    // }
    //
    // protected override IEnumerable<(EntProtoId spawn, double)> AverageSpawnsImplementation(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    // {
    //     return proto.Index(TableId).Table.AverageSpawns(entMan, proto, ctx);
    // }
    // Moffstation - End
}
