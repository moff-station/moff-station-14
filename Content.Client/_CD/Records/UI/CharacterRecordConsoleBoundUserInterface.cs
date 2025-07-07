using Content.Client.CriminalRecords;
using Content.Shared.CriminalRecords.Components;
using Content.Shared.CriminalRecords;
using Content.Shared.StationRecords;
using Content.Shared._CD.Records;
using JetBrains.Annotations;

namespace Content.Client._CD.Records.UI;

[UsedImplicitly]
public sealed class CharacterRecordConsoleBoundUserInterface(EntityUid owner, Enum key) : BoundUserInterface(owner, key)
{
    [ViewVariables] private CharacterRecordViewer? _window;
    private CrimeHistoryWindow? _historyWindow;

    protected override void UpdateState(BoundUserInterfaceState baseState)
    {
        base.UpdateState(baseState);
        if (baseState is not CharacterRecordConsoleState state)
            return;

        if (_window?.IsSecurity() ?? false)
        {
            var comp = EntMan.GetComponent<CriminalRecordsConsoleComponent>(Owner);
            _window!.SecurityWantedStatusMaxLength = comp.MaxStringLength;
        }

        _window?.UpdateState(state);
    }

    protected override void Open()
    {
        base.Open();

        _window = new CharacterRecordViewer(Owner);
        _window.OnClose += Close;
        _window.OnListingItemSelected += meta =>
        {
            SendMessage(new CharacterRecordConsoleSelectMsg(meta?.CharacterRecordKey));

            // If we are a security records console, we also need to inform the criminal records
            // system of our state.
            if (_window.IsSecurity() && meta?.StationRecordKey != null)
            {
                SendMessage(new SelectStationRecord(meta.Value.StationRecordKey.Value));
                _window.SetSecurityStatusEnabled(true);
            }
            else
            {
                // If the user does not have criminal records for some reason, we should not be able
                // to set their wanted status
                _window.SetSecurityStatusEnabled(false);
            }
        };

        _window.OnFiltersChanged += (ty, txt) =>
        {
            SendMessage(txt == null
                ? new CharacterRecordsConsoleFilterMsg(null)
                : new CharacterRecordsConsoleFilterMsg(new StationRecordsFilter(ty, txt)));
        };
        _window.OnStatusFilterPressed += statusFilter =>
            SendMessage(new CriminalRecordSetStatusFilter(statusFilter));

        _window.OnSetSecurityStatus += (status, reason) =>
        {
            SendMessage(new CriminalRecordChangeStatus(status, reason));
        };

        if (EntMan.TryGetComponent<CriminalRecordsConsoleComponent>(Owner, out var criminalRecords))
        {
            _window.OnHistoryUpdated += UpdateHistory;
            _window.OnHistoryClosed += () => _historyWindow?.Close();

            _historyWindow = new(criminalRecords.MaxStringLength);
            _historyWindow.OnAddHistory += line => SendMessage(new CriminalRecordAddHistory(line));
            _historyWindow.OnDeleteHistory += index => SendMessage(new CriminalRecordDeleteHistory(index));

            _historyWindow.Close(); // leave closed until user opens it
        }

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }

    /// <summary>
    /// Updates or opens a new history window.
    /// </summary>
    private void UpdateHistory(CriminalRecord record, bool access, bool open)
    {
        _historyWindow!.UpdateHistory(record, access);

        if (open)
            _historyWindow.OpenCentered();
    }
}
