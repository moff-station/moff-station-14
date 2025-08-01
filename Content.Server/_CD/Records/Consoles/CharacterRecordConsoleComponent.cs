using Content.Shared._CD.Records;
using Content.Shared.Security;
using Content.Shared.StationRecords;

namespace Content.Server._CD.Records.Consoles;

[RegisterComponent]
public sealed partial class CharacterRecordConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public uint? SelectedIndex { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public StationRecordsFilter? Filter;

    [ViewVariables(VVAccess.ReadOnly)]
    public SecurityStatus? SecurityStatusFilter;

    [DataField(required: true), ViewVariables(VVAccess.ReadOnly)]
    public RecordConsoleType ConsoleType;

    /// <summary>
    /// If true, the console will be able to view records off-station, allowing for remote access.
    /// </summary>
    [DataField]
    public bool LongRange;
}
