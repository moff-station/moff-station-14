using Content.Shared.CollectiveMind;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;

namespace Content.Shared.CollectiveMind;

public sealed class CollectiveMindUpdateSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    /// <summary>
    /// A map of <see cref="CollectiveMindPrototype">collective mind type</see> to the last anonymized ID number
    /// provisioned to a mind in that collective. This is used to allow the server to assign IDs to members of
    /// collective minds without reusing IDs.
    /// </summary>
    private static readonly Dictionary<ProtoId<CollectiveMindPrototype>, int> NextCollectiveMindId = new();
    /// <summary>
    /// Ensures <paramref name="entity"/>'s collective mind identities are properly initialized and any stray identities
    /// are removed.
    /// </summary>
    /// <param name="entity"></param>
    public void UpdateCollectiveMind(Entity<CollectiveMindComponent> entity)
    {
        foreach (var collectiveMindProto in _prototypeManager.EnumeratePrototypes<CollectiveMindPrototype>())
        {
            // Initialize the next ID if it's not already been initialized.
            NextCollectiveMindId.TryAdd(collectiveMindProto, 1);
            foreach (var collectiveMindReqComponent in collectiveMindProto.RequiredComponents)
            {
                EnsureCollectiveMind(
                    EntityManager.HasComponent(
                        entity,
                        _componentFactory.GetRegistration(collectiveMindReqComponent).Type
                    ),
                    entity.Comp,
                    collectiveMindProto);
            }
            foreach (var collectiveMindReqTag in collectiveMindProto.RequiredTags)
            {
                EnsureCollectiveMind(_tag.HasTag(entity, collectiveMindReqTag), entity.Comp, collectiveMindProto);
            }
        }
    }
    private static void EnsureCollectiveMind(
        bool shouldHave,
        CollectiveMindComponent comp,
        CollectiveMindPrototype proto
    )
    {
        if (shouldHave == comp.Minds.ContainsKey(proto))
            return;
        if (shouldHave)
        {
            comp.Minds.Add(proto, NextCollectiveMindId[proto]++);
        }
        else
        {
            comp.Minds.Remove(proto);
        }
    }
}
