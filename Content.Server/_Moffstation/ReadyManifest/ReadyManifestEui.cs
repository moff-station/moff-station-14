using Content.Server.EUI;
using Content.Shared._Moffstation.ReadyManifest;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.ReadyManifest;

public sealed class ReadyManifestEui(ReadyManifestSystem readyManifestSystem) : BaseEui
{
    public override ReadyManifestEuiState GetNewState()
    {
        var entries = new Dictionary<ProtoId<JobPrototype>, int>(readyManifestSystem.GetReadyManifest());
        return new ReadyManifestEuiState(entries);
    }

    public override void Closed()
    {
        readyManifestSystem.CloseEui(Player);
    }
}
