using Content.Server._Moffstation.Antag;
using Robust.Shared.Log;
using Robust.Shared.Network;

namespace Content.IntegrationTests._Moffstation.Antag;

public sealed class DummyWeightedAntagManager : IWeightedAntagManager
{
    private readonly ISawmill _logger = Logger.GetSawmill("antag_weight");

    public void Initialize()
    {
        _logger.Info("Using DummyWeightedAntagManager — antag weights will not be persisted");
    }
    public void Shutdown() { }
    public void SetWeight(NetUserId userId, int newWeight) { }
    public int GetWeight(NetUserId userId) => 0;
    public System.Threading.Tasks.Task Save() => System.Threading.Tasks.Task.CompletedTask;
}
