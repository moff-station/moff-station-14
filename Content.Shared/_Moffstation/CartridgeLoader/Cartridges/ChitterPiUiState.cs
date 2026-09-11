using Content.Shared._Moffstation.Chitter;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed partial class ChitterPiScanDoAfterEvent : SimpleDoAfterEvent
{
}

// Read-only snapshot pulled from a Chitter server's logs. Unlike ChitterUiState (the live PDA client), this is a
// one-shot capture that only changes when the cartridge is used to scan a server again.
[Serializable, NetSerializable]
public sealed class ChitterPiUiState : BoundUserInterfaceState
{
    public string ScannedServerName = string.Empty;
    public TimeSpan? LastScanTime;
    public List<ChitterLogChat> Chats = new();
}
