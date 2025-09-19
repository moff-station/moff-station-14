using System.Threading.Tasks;
using Content.Server.Administration.Notes;
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
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;    // they won't get updated the moment theyre added, but isn't that important because they're rare anyways
    }

    private readonly HashSet<ICommonSession> _watchLists = [];

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
        return _watchLists.Contains(session);
    }

    private void RemoveWatchlist(ICommonSession session)
    {
        _watchLists.Remove(session);
    }

    public async Task RefreshWatchlistBySession(ICommonSession session)
    {
        if ((await _notes.GetActiveWatchlists(session.UserId)).Count != 0)
            _watchLists.Add(session);
    }
}
