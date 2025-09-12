using Content.Server.Database;

namespace Content.Server._Moffstation.Players;

public sealed class WatchlistTrackingManager : IPostInjectInit
{
    [Dependency] private readonly UserDbDataManager _userDb = default!;
    private ISawmill _sawmill = default!;

    public void Shutdown()
    {
        Save();

        _task.BlockWaitOnTask(Task.WhenAll(_pendingSaveTasks));
    }

    public void Update()
    {
        // NOTE: This is run **out** of simulation. This is intentional.

        UpdateDirtyPlayers();

        if (_timing.RealTime < _lastSave + _saveInterval)
            return;

        Save();
    }

    void IPostInjectInit.PostInject()
    {
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
    }
}
