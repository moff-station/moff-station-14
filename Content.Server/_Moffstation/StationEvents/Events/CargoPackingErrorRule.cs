using Content.Server._Moffstation.StationEvents.Components;
using Content.Server._Moffstation.Storage.EntitySystems;
using Content.Server.StationEvents.Events;
using Content.Server.Storage.Components;
using Content.Shared._Moffstation.Cargo.Events;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Moffstation.StationEvents.Events;

/// <summary>
/// Handles <see cref="CargoPackingErrorRuleComponent"/>. This rule adds unexpected contents to cargo orders for
/// surprise and whimsy. Specifically, when the rule runs, it is added to a queue; when an item is ordered via cargo, if
/// that order has entity storage (eg. is a crate), the rule is dequeued and the entities are added to the order's
/// storage.
/// </summary>
public sealed class CargoPackingErrorRule : StationEventSystem<CargoPackingErrorRuleComponent>
{
    [Dependency] private readonly SpawnOnNextOpenSystem _spawnOnNextOpen = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly Queue<Entity<CargoPackingErrorRuleComponent, GameRuleComponent>> _activeRules = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityStorageComponent, CargoOrderFulfilledEvent>(OnCargoOrderFulfilledEvent);
    }

    protected override void Started(EntityUid uid,
        CargoPackingErrorRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!_prototype.HasIndex(component.SpawnTable))
        {
            DebugTools.Assert($"Failed to find spawn table with id \"{component.SpawnTable}\"");
            ForceEndSelf(uid);
        }
        else
        {
            _activeRules.Enqueue((uid, component, gameRule));
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

        var entitiesToSpawn = _entityTable.GetSpawns(spawnTable.Table, RobustRandom.GetRandom());
        _spawnOnNextOpen.AddEntitiesToSpawnOnFirstOpen(entity, entitiesToSpawn);

        ForceEndSelf(rule);
    }
}
