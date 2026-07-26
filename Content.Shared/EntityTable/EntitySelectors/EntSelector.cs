using Content.Shared.EntityTable.ValueSelector;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.EntitySelectors;

/// <summary>
/// A table which selects <see cref="Id"/>, <see cref="EntityTableSelector.Rolls"/> times.
/// </summary>
public sealed partial class EntSelector : EntityTableSelector
{
    public const string IdDataFieldTag = "id";

    /// <summary>
    /// The prototype this entry yields.
    /// </summary>
    // Moffstation - Begin - Rewrite table selectors with visitors. Early merge of https://github.com/Space-Wizards-Federation/space-station-14/pull/177
    // The const string is used in a specialized serializer.
#pragma warning disable RA0027
    [DataField(IdDataFieldTag, required: true)]
#pragma warning restore RA0027
    // Moffstation - End
    public EntProtoId Id;

    /// <summary>
    /// The amount of entities this entry might yield.
    /// </summary>
    [DataField]
    public NumberSelector Amount = new ConstantNumberSelector(1);

    // Moffstation - Begin - Rewrite table selectors with visitors. Early merge of https://github.com/Space-Wizards-Federation/space-station-14/pull/177
    public override TResult Accept<TContext, TResult>(IEntityTableVisitor<TContext, TResult> visitor, TContext args) =>
        visitor.VisitEntSelector(this, args);

    // protected override IEnumerable<EntProtoId> GetSpawnsImplementation(IRobustRandom rand,
    //     IEntityManager entMan,
    //     IPrototypeManager proto,
    //     EntityTableContext ctx)
    // {
    //     var num = Amount.Get(rand);
    //     for (var i = 0; i < num; i++)
    //     {
    //         yield return Id;
    //     }
    // }
    //
    // protected override IEnumerable<(EntProtoId spawn, double)> ListSpawnsImplementation(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    // {
    //     yield return (Id, 1f);
    // }
    //
    // protected override IEnumerable<(EntProtoId spawn, double)> AverageSpawnsImplementation(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    // {
    //     yield return (Id, Amount.Average());
    // }
    // Moffstation - End
}
