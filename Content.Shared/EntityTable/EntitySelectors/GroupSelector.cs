using Robust.Shared.Random;

namespace Content.Shared.EntityTable.EntitySelectors;

/// <summary>
/// Gets the spawns from one of <see cref="Children"/>, based on their <see cref="EntityTableSelector.Weight"/>s.
/// </summary>
public sealed partial class GroupSelector : EntityTableSelector
{
    /// <summary>
    /// The child entries of this selector.
    /// </summary>
    [DataField(required: true)]
    public List<EntityTableSelector> Children = new();

    // Moffstation - Begin - Rewrite table selectors with visitors. Early merge of https://github.com/Space-Wizards-Federation/space-station-14/pull/177
    /// <summary>
    /// If <see cref="EntityTableSelector.Rolls"/> is greater than 1, this determines if the multiple rolls can "hit"
    /// the same element in <see cref="Children"/> or not.
    /// </summary>
    /// <example>
    /// Consider a <c>GroupSelector</c> like this:
    /// <list type="bullet">
    /// <item>Rolls twice.</item>
    /// <item><c>A</c> with weight 9999999999</item>
    /// <item><c>B</c> with weight 1</item>
    /// </list>
    /// When <see cref="EntityTableSystem.GetSpawns(EntityTableSelector?, IRobustRandom?, EntityTableContext?)">GetSpawns</see>
    /// is called on this table, we would expect it to yield <c>A</c> twice virtually every time.
    /// <br/>
    /// If we take the same table and set <see cref="CanRollDuplicates"/> to <c>false</c>, it will definitely yield
    /// <c>A</c> and <c>B</c> once each, as <c>A</c> cannot be rolled twice.
    /// </example>
    [DataField]
    public bool CanRollDuplicates = true;

    public override TResult Accept<TContext, TResult>(IEntityTableVisitor<TContext, TResult> visitor, TContext args) =>
        visitor.VisitGroupSelector(this, args);

    // protected override IEnumerable<EntProtoId> GetSpawnsImplementation(IRobustRandom rand,
    //     IEntityManager entMan,
    //     IPrototypeManager proto,
    //     EntityTableContext ctx)
    // {
    //     var children = new Dictionary<EntityTableSelector, float>(Children.Count);
    //     foreach (var child in Children)
    //     {
    //         // Don't include invalid groups
    //         if (!child.CheckConditions(entMan, proto, ctx))
    //             continue;
    //
    //         children.Add(child, child.Weight);
    //     }
    //
    //     if (children.Count == 0)
    //         return Array.Empty<EntProtoId>();
    //
    //     var pick = SharedRandomExtensions.Pick(children, rand);
    //
    //     return pick.GetSpawns(rand, entMan, proto, ctx);
    // }
    //
    // protected override IEnumerable<(EntProtoId spawn, double)> ListSpawnsImplementation(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    // {
    //     var totalWeight = Children.Sum(x => x.Weight);
    //
    //     foreach (var child in Children)
    //     {
    //         var weightMod = child.Weight / totalWeight;
    //         foreach (var (ent, prob) in child.ListSpawns(entMan, proto, ctx, weightMod))
    //         {
    //             yield return (ent, prob);
    //         }
    //     }
    // }
    //
    // protected override IEnumerable<(EntProtoId spawn, double)> AverageSpawnsImplementation(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    // {
    //     var totalWeight = Children.Sum(x => x.Weight);
    //
    //     foreach (var child in Children)
    //     {
    //         var weightMod = child.Weight / totalWeight;
    //         foreach (var (ent, prob) in child.AverageSpawns(entMan, proto, ctx, weightMod))
    //         {
    //             yield return (ent, prob);
    //         }
    //     }
    // }
    // Moffstation - End
}
