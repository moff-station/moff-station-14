using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Notes;
using Content.Server.Database;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server._Moffstation.Players;

/// <summary>
/// This is a class that allows synchronous code to identify whether a player is WatchListed
/// </summary>
public sealed class WatchListTracker : EntitySystem
{
    [Dependency] private readonly IAdminNotesManager _notes = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private readonly Dictionary<ICommonSession, bool> _watchLists = new();

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        switch (e.NewStatus)
        {
            case SessionStatus.Connected:
                await RefreshWatchlistBySession(e.Session);
                break;
            default:
                RemoveWatchlist(e.Session);
                break;
        }
    }

    public bool GetWatchListed(ICommonSession session)
    {
        return _watchLists.TryGetValue(id, out var data) && data;
    }

    private void RemoveWatchlist(ICommonSession session)
    {
        _watchLists.Remove(session);
    }

    public async Task RefreshWatchlistBySession(ICommonSession session)
    {
        _watchLists[session] = (await _notes.GetActiveWatchlists(session.UserId)).Any();
    }
}
