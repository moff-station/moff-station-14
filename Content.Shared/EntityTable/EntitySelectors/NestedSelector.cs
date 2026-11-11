using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.EntitySelectors;

/// <summary>
/// A table which simply delegates to the table identified by <see cref="TableId"/>.
/// Can be used to reuse common tables.
/// </summary>
public sealed partial class NestedSelector : EntityTableSelectorWithNestedBase
{
    /// <summary>
    /// The prototype from which to draw random items.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<EntityTablePrototype> TableId;

    /// <inheritdoc/>>
    public override bool CheckConditions(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        using var scoped = ScopedConditions(ctx);
        return base.CheckConditions(entMan, proto, ctx) && proto.Index(TableId).Table.CheckConditions(entMan, proto, ctx);
    }

    // Moffstation - Begin - Rewrite table selectors with visitors. Early merge of https://github.com/Space-Wizards-Federation/space-station-14/pull/177
    public override TResult Accept<TContext, TResult>(IEntityTableVisitor<TContext, TResult> visitor, TContext args) =>
        visitor.VisitNestedSelector(this, args);

    // /// <inheritdoc/>>
    // protected override IEnumerable<EntProtoId> GetSpawnsImplementation(IRobustRandom rand,
    //     IEntityManager entMan,
    //     IPrototypeManager proto,
    //     EntityTableContext ctx)
    // {
    //     using var scoped = ScopedConditions(ctx);
    //
    //     foreach (var spawn in proto.Index(TableId).Table.GetSpawns(rand, entMan, proto, ctx))
    //     {
    //         yield return spawn;
    //     }
    // }
    //
    // /// <inheritdoc/>>
    // protected override IEnumerable<(EntProtoId spawn, double)> ListSpawnsImplementation(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    // {
    //     return proto.Index(TableId).Table.ListSpawns(entMan, proto, ctx);
    // }
    //
    // /// <inheritdoc/>>
    // protected override IEnumerable<(EntProtoId spawn, double)> AverageSpawnsImplementation(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    // {
    //     return proto.Index(TableId).Table.AverageSpawns(entMan, proto, ctx);
    // }
    // Moffstation - End

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Nested({TableId})";
    }
}
