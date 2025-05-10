using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Server.StationRecords;
using Content.Shared.CriminalRecords;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Content.Shared._CD.Records;
using Robust.Server.GameObjects;

namespace Content.Server._CD.Records.Consoles;

public sealed class CharacterRecordConsoleSystem : EntitySystem
{
    [Dependency] private readonly CharacterRecordsSystem _characterRecords = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CharacterRecordConsoleComponent, CharacterRecordsModifiedEvent>((uid, component, _) =>
            UpdateUi(uid, component));

        Subs.BuiEvents<CharacterRecordConsoleComponent>(CharacterRecordConsoleKey.Key,
            subr =>
            {
                subr.Event<BoundUIOpenedEvent>((uid, component, _) => UpdateUi(uid, component));
                subr.Event<CharacterRecordConsoleSelectMsg>(OnKeySelect);
                subr.Event<CharacterRecordsConsoleFilterMsg>(OnFilterApplied);
                subr.Event<CriminalRecordSetStatusFilter>(OnSecurityFilterApplied);
                subr.Event<CriminalRecordAddHistory>(OnAddCriminalHistory);
            });
    }

    private void OnFilterApplied(Entity<CharacterRecordConsoleComponent> ent, ref CharacterRecordsConsoleFilterMsg msg)
    {
        ent.Comp.Filter = msg.Filter;
        UpdateUi(ent);
    }

    private void OnSecurityFilterApplied(Entity<CharacterRecordConsoleComponent> ent,
        ref CriminalRecordSetStatusFilter msg)
    {
        ent.Comp.SecurityStatusFilter = msg.FilterStatus;
        UpdateUi(ent);
    }

    private void OnKeySelect(Entity<CharacterRecordConsoleComponent> ent, ref CharacterRecordConsoleSelectMsg msg)
    {
        ent.Comp.SelectedIndex = msg.CharacterRecordKey;
        UpdateUi(ent);
    }

    private void OnAddCriminalHistory(Entity<CharacterRecordConsoleComponent> ent, ref CriminalRecordAddHistory msg)
    {
        UpdateUi(ent, ent);
    }

    private void UpdateUi(EntityUid entity, CharacterRecordConsoleComponent? console = null)
    {
        if (!Resolve(entity, ref console))
            return;

        var station = _station.GetOwningStation(entity);
        if (!HasComp<StationRecordsComponent>(station) ||
            !HasComp<CharacterRecordsComponent>(station))
        {
            SendState(entity, new CharacterRecordConsoleState { ConsoleType = console.ConsoleType });
            return;
        }

        var characterRecords = _characterRecords.QueryRecords(station.Value);
        // Get the name and station records key display from the list of records
        var names = new Dictionary<uint, CharacterRecordConsoleState.CharacterInfo>();
        foreach (var (i, r) in characterRecords)
        {
            var netEnt = _entity.GetNetEntity(r.Owner!.Value);
            // Admins get additional info to make it easier to run commands
            var nameJob = console.ConsoleType != RecordConsoleType.Admin
                ? $"{r.Name} ({r.JobTitle})"
                : $"{r.Name} ({netEnt}, {r.JobTitle}";

            // Apply any filter the user has set
            if (console.Filter != null)
            {
                if (IsSkippedRecord(console.Filter, r, nameJob))
                    continue;
            }

            if (console.SecurityStatusFilter is {} secFilter &&
                station is {} s &&
                IsSkippedBySecurityStatus(secFilter, r, s))
            {
                continue;
            }

            if (names.ContainsKey(i))
            {
                Log.Error(
                    $"We somehow have duplicate character record keys, NetEntity: {i}, Entity: {entity}, Character Name: {r.Name}");
            }

            names[i] = new CharacterRecordConsoleState.CharacterInfo
                { CharacterDisplayName = nameJob, StationRecordKey = r.StationRecordsKey };
        }

        var record =
            console.SelectedIndex == null || !characterRecords.TryGetValue(console.SelectedIndex!.Value, out var value)
                ? null
                : value;
        CriminalRecord? criminalRecord = null;

        // If we need the character's security status, gather it from the criminal records
        if ((console.ConsoleType == RecordConsoleType.Admin ||
             console.ConsoleType == RecordConsoleType.Security)
            && record?.StationRecordsKey != null)
        {
            var key = new StationRecordKey(record.StationRecordsKey.Value, station.Value);
            _records.TryGetRecord(key, out criminalRecord);
        }

        SendState(entity,
            new CharacterRecordConsoleState
            {
                ConsoleType = console.ConsoleType,
                CharacterList = names,
                SelectedIndex = console.SelectedIndex,
                SelectedRecord = record,
                Filter = console.Filter,
                SelectedCriminalRecord = criminalRecord,
            });
    }

    private void SendState(EntityUid entity, CharacterRecordConsoleState state)
    {
        _ui.SetUiState(entity, CharacterRecordConsoleKey.Key, state);
    }

    /// <summary>
    /// Almost exactly the same as <see cref="StationRecordsSystem.IsSkipped"/>
    /// </summary>
    private static bool IsSkippedRecord(StationRecordsFilter filter,
        FullCharacterRecords record,
        string nameJob)
    {
        var isFilter = filter.Value.Length > 0;

        if (!isFilter)
            return false;

        var filterLowerCaseValue = filter.Value.ToLower();

        return filter.Type switch
        {
            StationRecordFilterType.Name =>
                !nameJob.Contains(filterLowerCaseValue, StringComparison.CurrentCultureIgnoreCase),
            StationRecordFilterType.Prints => record.Fingerprint != null
                && IsFilterWithSomeCodeValue(record.Fingerprint, filterLowerCaseValue),
            StationRecordFilterType.DNA => record.DNA != null
                                                && IsFilterWithSomeCodeValue(record.DNA, filterLowerCaseValue),
            _ => throw new ArgumentOutOfRangeException(nameof(filter), "Invalid Character Record filter type"),
        };
    }

    private bool IsSkippedBySecurityStatus(SecurityStatus filter, FullCharacterRecords record, EntityUid station)
    {
        // "None" filter is "Show everything"
        if (filter == SecurityStatus.None)
            return false;

        var criminalStatusOfRecord = record.StationRecordsKey is { } key &&
                                     _records.TryGetRecord<CriminalRecord>(new StationRecordKey(key, station),
                                         out var criminalRec)
            ? criminalRec.Status
            : SecurityStatus.None;

        return filter != criminalStatusOfRecord;
    }

    private static bool IsFilterWithSomeCodeValue(string value, string filter)
    {
        return !value.StartsWith(filter, StringComparison.CurrentCultureIgnoreCase);
    }
}
