using System.Collections.Immutable;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Moffstation.Chitter.Probe;

/// This component makes its owner a Chitter Probe cart/program, which allows for the user to download
/// <see cref="ChitterProbeData"/> from chitter servers.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChitterProbeCartridgeComponent : Component
{
    /// The most recently accessed data. Null if none has been downloaded yet.
    [DataField, AutoNetworkedField]
    public ChitterProbeData? ProbeData;

    /// The duration of the doafter to download data from a chitter server.
    [DataField, AutoNetworkedField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(3);

    /// The sound to play when data is downloaded.
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ScanSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    /// The popup to show when data is downloaded.
    [DataField, AutoNetworkedField]
    public LocId? ScanPopup = "chitter-pi-scan-complete";
}

/// <summary>
/// The data taken from a chitter server by a Chitter Probe.
/// </summary>
/// <param name="ServerName">The name of the scanned server entity</param>
/// <param name="Chats">The chats taken from the server</param>
/// <param name="ScannedAt">When the server was scanned.</param>
[DataRecord, Serializable, NetSerializable]
public partial record struct ChitterProbeData(
    string ServerName,
    ImmutableArray<ChitterUiChatDetail> Chats,
    ImmutableArray<ChitterAccount> Accounts,
    [field: DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    TimeSpan ScannedAt
);

/// The BUI for a <see cref="ChitterProbeCartridgeComponent"/>'s UI.
[Serializable, NetSerializable]
public sealed class ChitterProbeUiState(ChitterProbeData? data) : BoundUserInterfaceState
{
    public readonly ChitterProbeData? Data = data;
}

/// The doafter event for <see cref="ChitterProbeCartridgeComponent"/>'s downloading.
[Serializable, NetSerializable]
public sealed partial class ChitterProbeScanDoAfterEvent : SimpleDoAfterEvent;
