using Content.Shared._Moffstation.Chitter;

namespace Content.Server._Moffstation.CartridgeLoader.Cartridges;

[RegisterComponent, Access(typeof(ChitterPiCartridgeSystem))]
public sealed partial class ChitterPiCartridgeComponent : Component
{
    [DataField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(3);

    public string ScannedServerName = string.Empty;
    public TimeSpan? LastScanTime;
    public List<ChitterLogChat> Chats = new();
}
