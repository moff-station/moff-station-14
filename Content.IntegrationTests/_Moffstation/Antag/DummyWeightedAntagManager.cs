using Content.Server._Moffstation.Antag;
using Robust.Shared.Network;

namespace Content.IntegrationTests._Moffstation.Antag;

public sealed class DummyWeightedAntagManager : IWeightedAntagManager
{
    public void Initialize() { }
    public void Shutdown() { }
    public void SetWeight(NetUserId userId, int newWeight) { }
    public int GetWeight(NetUserId userId) => 0;
    public System.Threading.Tasks.Task Save() => System.Threading.Tasks.Task.CompletedTask;
}
