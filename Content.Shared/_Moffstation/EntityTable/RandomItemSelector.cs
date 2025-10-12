using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Item;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Moffstation.EntityTable;

public sealed partial class RandomItemSelector : EntityTableSelector
{
    private readonly HashSet<EntProtoId> _validPrototypes = new();

    protected override IEnumerable<EntProtoId> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        if (_validPrototypes.Count == 0)
        {
            foreach (var prototype in proto.EnumeratePrototypes<EntityPrototype>())
            {
                if (!prototype.Components.ContainsKey(entMan.ComponentFactory.GetComponentName<ItemComponent>()))
                    continue;

                _validPrototypes.Add(prototype.ID);
            }
        }

        var spawn = new List<EntProtoId> {rand.Pick(_validPrototypes)};
        return spawn;
    }
}
