using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._Moffstation.AdminGhost;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Moffstation.AdminGhost;

public sealed partial class AdminGhostSaveManager : IPostInjectInit
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private ILogManager _log = default!;
    [Dependency] private UserDbDataManager _userDb = default!;

    private ISawmill _sawmill = default!;
    private readonly ConcurrentDictionary<NetUserId, AdminGhostSavedData?> _cachedData = new();

    void IPostInjectInit.PostInject()
    {
        _sawmill = _log.GetSawmill("admin_ghost_save");
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnFinishLoad(FinishLoad);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
    }

    public AdminGhostSavedData? GetData(NetUserId userId)
    {
        return _cachedData.GetOrAdd(userId, id =>
        {
            var json = Task.Run(() => _db.GetAdminGhostData(id)).GetAwaiter().GetResult();
            if (string.IsNullOrEmpty(json))
                return null;
            try
            {
                return JsonSerializer.Deserialize<AdminGhostSavedData>(json);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Failed to deserialize admin ghost data for {id}: {e}");
                return null;
            }
        });
    }

    public async Task<bool> SetData(NetUserId userId, AdminGhostSavedData? data)
    {
        var json = data != null ? JsonSerializer.Serialize(data) : null;
        _cachedData[userId] = data;
        return await _db.SetAdminGhostData(userId, json);
    }

    public void RemoveData(NetUserId userId)
    {
        _cachedData.TryRemove(userId, out _);
    }

    public async Task LoadData(ICommonSession session, CancellationToken cancel)
    {
        var userId = session.UserId;
        var json = await _db.GetAdminGhostData(userId);

        if (string.IsNullOrEmpty(json))
        {
            _cachedData[userId] = null;
            return;
        }

        try
        {
            var data = JsonSerializer.Deserialize<AdminGhostSavedData>(json);
            _cachedData[userId] = data;
            _sawmill.Debug($"Loaded admin ghost data for {userId}");
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to deserialize admin ghost data for {userId}: {e}");
            _cachedData[userId] = null;
        }
    }

    public void FinishLoad(ICommonSession session)
    {
        // No-op - data is already loaded
    }

    public void ClientDisconnected(ICommonSession session)
    {
        RemoveData(session.UserId);
    }
}
