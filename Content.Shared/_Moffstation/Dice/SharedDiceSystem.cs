using Content.Shared._Moffstation.Extensions;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;

namespace Content.Shared.Dice;

public partial class SharedDiceSystem
{
    [Dependency] private EntityQuery<DiceComponent> _diceQuery;

    [SubscribeLocalEvent]
    private void OnDumped(Entity<StorageComponent> ent, ref DumpEvent args)
    {
        foreach (var entity in _diceQuery.ResolveAll(args.DumpQueue, logMissing: false))
        {
            Roll(entity);
        }
    }
}
