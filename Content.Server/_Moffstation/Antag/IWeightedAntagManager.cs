using System.Threading.Tasks;
using Robust.Shared.Network;

namespace Content.Server._Moffstation.Antag;

public interface IWeightedAntagManager
{
    void Initialize();
    void Shutdown();
    void SetWeight(NetUserId userId, int newWeight);
    int GetWeight(NetUserId userId);
    Task Save();
}
