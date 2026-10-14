using Content.Server._Moffstation.StationEvents.Components;
using Content.Server._Moffstation.Storage.EntitySystems;
using Content.Server.StationEvents.Events;
using Content.Shared._Moffstation.Cargo.Events;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Content.Shared.Storage.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Moffstation.StationEvents.Events;

/// <summary>
/// Handles <see cref="CargoPackingErrorRuleComponent"/>. This rule adds unexpected contents to cargo orders for
/// surprise and whimsy. Specifically, when the rule runs, it is added to a queue; when an item is ordered via cargo, if
/// that order has entity storage (eg. is a crate), the rule is dequeued and the entities are added to the order's
/// storage.
/// </summary>
public sealed partial class CargoPackingErrorRule : StationEventSystem<CargoPackingErrorRuleComponent>
{
    [Dependency] private SpawnOnNextOpenSystem _spawnOnNextOpen = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    private readonly Queue<Entity<CargoPackingErrorRuleComponent, GameRuleComponent>> _activeRules = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityStorageComponent, CargoOrderFulfilledEvent>(OnCargoOrderFulfilledEvent);
    }

    protected override void Started(Entity<CargoPackingErrorRuleComponent, GameRuleComponent> rule, ref GameRuleStartedEvent args)
    {
        base.Started(rule, ref args);

        if (!_prototype.HasIndex(rule.Comp1.SpawnTable))
        {
            DebugTools.Assert($"Failed to find spawn table with id \"{rule.Comp1.SpawnTable}\"");
            ForceEndSelf((rule.Owner, rule.Comp2));
        }
        else
        {
            _activeRules.Enqueue(rule);
        }
    }

    private void OnCargoOrderFulfilledEvent(Entity<EntityStorageComponent> entity, ref CargoOrderFulfilledEvent args)
    {
        if (
            !_activeRules.TryDequeue(out var rule) ||
            !_prototype.TryIndex(rule.Comp1.SpawnTable, out var spawnTable)
        )
        {
            return;
        }

        var entitiesToSpawn = _entityTable.GetSpawns(spawnTable.Table, RobustRandom);
        _spawnOnNextOpen.AddEntitiesToSpawnOnFirstOpen(entity, entitiesToSpawn);

        ForceEndSelf((rule.Owner, rule.Comp2));
    }
}
