using Content.Server._CD.Spawners;
using Content.Server.Access.Systems;
using Content.Server.GameTicking;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.StationRecords;

namespace Content.Server._CD.Loadouts;

public sealed class RenameIdSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;

    public override void Initialize()
    {
        base.Initialize();

        // We need to subscribe to both of these because RulePlayerJobsAssignedEvent only fires on round start and
        // messes up what we do in PlayerSpawnCompleteEvent
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnJobsAssigned, after: [ typeof(PresetIdCardSystem), typeof(ArrivalsSpawnPointSystem) ]);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn, after: [ typeof(StationRecordsSystem), typeof(ArrivalsSpawnPointSystem) ]);
    }

    private void OnJobsAssigned(RulePlayerJobsAssignedEvent args)
    {
        var query = EntityQuery<RenameIdComponent, PdaComponent>();
        foreach (var (rename, pda) in query)
        {
            if (pda.ContainedId is not { } id ||
                !TryComp<IdCardComponent>(id, out var card))
                continue;

            UpdateIdByComp((id, card), rename);
        }
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        var player = args.Mob;

        if (!_inventorySystem.TryGetSlotEntity(player, "id", out var pdaUid) ||
            !TryComp<PdaComponent>(pdaUid, out var pda) ||
            !TryComp<RenameIdComponent>(pdaUid, out var rename) ||
            pda.ContainedId is not { } id ||
            !TryComp<IdCardComponent>(id, out var card))
            return;

        UpdateIdByComp((id, card), rename);
    }

    private void UpdateIdByComp(Entity<IdCardComponent> id, RenameIdComponent comp)
    {
        if (comp.NewIcon is { } icon)
        {
            id.Comp.JobIcon = icon; // TryChangeJobTitle dirties the ID for us
            if (TryComp<StationRecordKeyStorageComponent>(id, out var keyStorage) &&
                keyStorage.Key is { } key &&
                _records.TryGetRecord<GeneralStationRecord>(key, out var record))
            {
                record.JobIcon = icon;
                _records.Synchronize(key);
            }
        }
        _idCardSystem.TryChangeJobTitle(id, Loc.GetString(comp.Value), id);
    }
}
