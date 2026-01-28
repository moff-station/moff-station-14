using Content.Shared.Eui;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.ReadyManifest;

/// <summary>
///     A message to send to the server when requesting a ready manifest.
///     ReadyManifestSystem will open an EUI that will be updated whenever
///     a player changes their ready status.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestReadyManifestMessage : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class ReadyManifestEuiState(Dictionary<ProtoId<JobPrototype>, int> jobCounts) : EuiStateBase
{
    public readonly Dictionary<ProtoId<JobPrototype>, int> JobCounts = jobCounts;
}
