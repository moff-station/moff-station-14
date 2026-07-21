using System.Linq;
using Content.Shared.EntityTable.EntitySelectors;
using JetBrains.Annotations;

namespace Content.Shared.EntityTable;

// Moffstation - Begin - Rewrite table selectors with visitors. Early merge of https://github.com/Space-Wizards-Federation/space-station-14/pull/177
/// <summary>
/// <a href="https://en.wikipedia.org/wiki/Visitor_pattern">Visitor</a> for <see cref="EntityTableSelector"/>s.
/// </summary>
/// <typeparam name="TArgs">The type of arguments passed to visitation</typeparam>
/// <typeparam name="TResult">The type of the visitation result</typeparam>
[PublicAPI]
public interface IEntityTableVisitor<in TArgs, out TResult>
{
    /// <summary>
    /// Alias of <see cref="EntityTableSelector.Accept{TContext, TResult}(IEntityTableVisitor{TContext, TResult}, TContext)"/>.
    /// </summary>
    [PublicAPI]
    TResult Visit(EntityTableSelector selector, TArgs args) => selector.Accept(this, args);

    /// <summary>
    /// Visit an <see cref="AllSelector"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitAllSelector(AllSelector selector, TArgs args);

    /// <summary>
    /// Visit an <see cref="EntSelector"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitEntSelector(EntSelector selector, TArgs args);

    /// <summary>
    /// Visit a <see cref="GroupSelector"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitGroupSelector(GroupSelector selector, TArgs args);

    /// <summary>
    /// Visit a <see cref="NestedSelector"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitNestedSelector(NestedSelector selector, TArgs args);

    /// <summary>
    /// Visit a <see cref="NoneSelector"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitNoneSelector(NoneSelector selector, TArgs args);
}

public static class IEntityTableVisitor
{
    /// This is the shared implementation for <see cref="GroupSelector"/>'s <c>ListSpawns</c> and <c>AverageSpawns</c>
    /// visitors.
    public static IEnumerable<(EntityTableSelector child, float prob)> VisitGroupSelectorNoDupesImpl(
        List<EntityTableSelector> children,
        float expectedRollsLeft
    )
    {
        var selectorsToConsider = children;
        while (true)
        {
            if (selectorsToConsider.Count == 0 || expectedRollsLeft <= float.Epsilon)
                yield break;

            var sumOfChildWeights = selectorsToConsider.Sum(c => c.Weight);

            // A selector is "certain" to be picked when its proportional rate exceeds 1 -- it would claim more than one
            // roll under proportional allocation, so it's guaranteed to appear and is capped at probability 1.
            var certainPicks = new List<EntityTableSelector>();
            var uncertainPicks = new List<EntityTableSelector>();
            foreach (var c in selectorsToConsider)
            {
                var list = c.Weight * expectedRollsLeft >= sumOfChildWeights ? certainPicks : uncertainPicks;
                list.Add(c);
            }

            // If there are no guaranteed picks, yield all selectors with their weights modulated.
            if (certainPicks.Count == 0)
            {
                var weightModifier = expectedRollsLeft / sumOfChildWeights;
                foreach (var c in selectorsToConsider)
                {
                    yield return (c, c.Weight * weightModifier);
                }

                yield break;
            }

            // Otherwise...
            // Yield all guaranteed picks with a probability of 1.
            foreach (var c in certainPicks)
            {
                yield return (c, 1f);
            }

            // And now yield all remaining selectors with their weights modulated by the remaining roll probability.
            // (Note that this is done in a loop that implements effectively a tail-recursive call of this function)
            selectorsToConsider = uncertainPicks;
            expectedRollsLeft -= certainPicks.Count;
        }
    }
}
// Moffstation - End
