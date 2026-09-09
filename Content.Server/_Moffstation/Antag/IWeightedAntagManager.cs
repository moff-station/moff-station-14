using System.Threading.Tasks;
using Robust.Shared.Network;

namespace Content.Server._Moffstation.Antag;

public interface IWeightedAntagManager
{
    void Initialize();
    void Shutdown();
    void SetWeight(NetUserId userId, int newWeight);
    void IncrementWeight(NetUserId userId, int amount = 1);
    int GetWeight(NetUserId userId);
    Task Save();
}
