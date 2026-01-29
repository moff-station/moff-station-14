using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Shared.Asynchronous;
using Robust.Shared.Network;

namespace Content.Server._Moffstation.Antag;

public sealed class WeightedAntagManager
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;

    private readonly List<Task> _pendingSaveTasks = new();
    private ISawmill _logger = default!;
    private readonly Dictionary<NetUserId, int> _cachedAntagWeight = new();

    public void Initialize()
    {
        _logger = Logger.GetSawmill("antag_weight");
    }

    public void Shutdown()
    {
        Save();
        _taskManager.BlockWaitOnTask(Task.WhenAll(_pendingSaveTasks));
    }

    public void SetWeight(NetUserId userId, int newWeight)
    {
        var oldWeight = GetWeight(userId);
        _cachedAntagWeight[userId] = newWeight;

        _logger.Info($"Updated antag weight for {userId}: {oldWeight} -> {newWeight}");
    }

    public void Save()
    {
        foreach (var user in _cachedAntagWeight)
        {
            _ = SaveWeight(user.Key, user.Value);
        }
    }

    private async Task<int> SaveWeight(NetUserId userId, int newWeight)
    {
        var oldWeight = GetWeight(userId);
        _cachedAntagWeight[userId] = newWeight;
        var saveTask = _db.SetAntagWeight(userId, newWeight);
        RegisterShutdownTask(saveTask);

        if (await saveTask)
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

    public int GetWeight(NetUserId userId)
    {
        if (_cachedAntagWeight.TryGetValue(userId, out var weight))
            return weight;

        weight = Task
            .Run(() => _db.GetAntagWeight(userId))
            .GetAwaiter()
            .GetResult();
        _cachedAntagWeight.Add(userId, weight);
        return weight;
    }

    private async void RegisterShutdownTask(Task task)
    {
        _pendingSaveTasks.Add(task);

        try
        {
            await task;
        }
        finally
        {
            _pendingSaveTasks.Remove(task);
        }
    }
}

