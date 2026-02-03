using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Shared.Asynchronous;
using Robust.Shared.Network;

namespace Content.Server._Moffstation.Antag;

public sealed class WeightedAntagManager
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;

    private ISawmill _logger = default!;
    private readonly ConcurrentDictionary<NetUserId, int> _cachedAntagWeight = new();

    public void Initialize()
    {
        _logger = Logger.GetSawmill("antag_weight");
    }

    public void Shutdown()
    {
        _taskManager.BlockWaitOnTask(Save());
    }

    public void SetWeight(NetUserId userId, int newWeight)
    {
        var oldWeight = GetWeight(userId);
        _cachedAntagWeight[userId] = newWeight;

        _logger.Info($"Updated antag weight for {userId}: {oldWeight} -> {newWeight}");
    }

    public async Task Save()
    {
        // Defensive copy to avoid concurrent modification during iteration
        var weights = _cachedAntagWeight.ToArray();
        var tasks = weights.Select(it => SaveWeight(it.Key, it.Value));
        await Task.WhenAll(tasks).ConfigureAwait(false); // `ConfigureAwait(false)` basically says that we don't need the context / thread from before and to run the await and subsequent code wherever.
    }

    private async Task<int> SaveWeight(NetUserId userId, int newWeight)
    {
        var oldWeight = GetWeight(userId);
        _cachedAntagWeight[userId] = newWeight;
        var saveTask = _db.SetAntagWeight(userId, newWeight);

        if (await saveTask.ConfigureAwait(false))
        {
            _logger.Debug(
                $"Antag weight saved for {userId}: {oldWeight} -> {newWeight}");
        }
        else
        {
            _logger.Error(
                $"Failed to persist antag weight for {userId}");
        }

        return oldWeight;
    }

    public int GetWeight(NetUserId userId) => _cachedAntagWeight.GetOrAdd(
        userId,
        _ => Task
            .Run(() => _db.GetAntagWeight(userId))
            .GetAwaiter()
            .GetResult()
    );
}
