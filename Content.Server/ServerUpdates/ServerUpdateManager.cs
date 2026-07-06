using System.Linq;
using Content.Server.Chat.Managers;
using Content.Shared._Moffstation.CCVar;
using Content.Shared.CCVar;
using Robust.Server;
using Robust.Server.Player;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.ServerUpdates;

/// <summary>
/// Responsible for restarting the server periodically or for update, when not disruptive.
/// </summary>
/// <remarks>
/// This was originally only designed for restarting on *update*,
/// but now also handles periodic restarting to keep server uptime via <see cref="CCVars.ServerUptimeRestartMinutes"/>.
/// </remarks>
public sealed partial class ServerUpdateManager : IPostInjectInit
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IWatchdogApi _watchdog = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IBaseServer _server = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    [ViewVariables]
    private bool _updateOnRoundEnd;

    private TimeSpan? _restartTime;

    private TimeSpan _uptimeRestart;

    // Moff Start - pause-triggered restart
    private TimeSpan? _restartQueueTime;
    private TimeSpan? _restartQueueNextAnnounce;

    private bool _restartQueueEnabled;
    private TimeSpan _restartQueueRestartDelay;
    private TimeSpan _restartQueueAnnounceInterval;
    private TimeSpan _restartQueueFinalAnnounceInterval;
    private TimeSpan _restartQueueFinalAnnounceThreshold;
    // Moff end

    public void Initialize()
    {
        _watchdog.UpdateReceived += WatchdogOnUpdateReceived;
        _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;

        _cfg.OnValueChanged(
            CCVars.ServerUptimeRestartMinutes,
            minutes => _uptimeRestart = TimeSpan.FromMinutes(minutes),
            true);

        // Moff Start - pause-triggered restart
        _cfg.OnValueChanged(
            MoffCCVars.RestartQueueEnabled,
            enabled => _restartQueueEnabled = enabled,
            true);
        _cfg.OnValueChanged(
            MoffCCVars.RestartQueueTimer,
            minutes => _restartQueueRestartDelay = TimeSpan.FromMinutes(minutes),
            true);
        _cfg.OnValueChanged(
            MoffCCVars.PauseRestartAnnounceInterval,
            minutes => _restartQueueAnnounceInterval = TimeSpan.FromMinutes(minutes),
            true);
        _cfg.OnValueChanged(
            MoffCCVars.PauseRestartFinalAnnounceInterval,
            minutes => _restartQueueFinalAnnounceInterval = TimeSpan.FromMinutes(minutes),
            true);
        _cfg.OnValueChanged(
            MoffCCVars.PauseRestartFinalAnnounceThreshold,
            minutes => _restartQueueFinalAnnounceThreshold = TimeSpan.FromMinutes(minutes),
            true);
        // Moff end
    }

    public void Update()
    {
        if (_restartTime != null)
        {
            if (_restartTime < _gameTiming.RealTime)
            {
                DoShutdown();
            }
        }
        else
        {
            if (ShouldShutdownDueToUptime())
            {
                ServerEmptyUpdateRestartCheck("uptime");
            }

            UpdatePauseRestart(); // Moff - pause-triggered restart
        }
    }

    // Moff Start - pause-triggered restart
    private void UpdatePauseRestart()
    {
        if (!_restartQueueEnabled
            || !_gameTiming.Paused
            || !(_updateOnRoundEnd || ShouldShutdownDueToUptime())
            || _playerManager.Sessions.All(p => p.Status == SessionStatus.Disconnected))
        {
            if (_restartQueueTime == null)
                return;

            _sawmill.Debug("Aborting pause-triggered restart timer due to unpause or restart no longer being due");
            _restartQueueTime = null;
            _restartQueueNextAnnounce = null;

            return;
        }

        if (_restartQueueTime == null)
        {
            _restartQueueTime = _gameTiming.RealTime + _restartQueueRestartDelay;
            _restartQueueNextAnnounce = _restartQueueRestartDelay;
            _sawmill.Debug("Started pause-triggered restart timer due to game being paused with players connected");
        }

        var remaining = _restartQueueTime.Value - _gameTiming.RealTime;
        if (remaining <= TimeSpan.Zero)
        {
            DoShutdown();
            return;
        }

        if (_restartQueueNextAnnounce == null || !(remaining <= _restartQueueNextAnnounce))
            return;

        AnnouncePauseRestart(remaining);

        var step = _restartQueueNextAnnounce > _restartQueueFinalAnnounceThreshold
            ? _restartQueueAnnounceInterval
            : _restartQueueFinalAnnounceInterval;
        var next = _restartQueueNextAnnounce.Value - step;
        _restartQueueNextAnnounce = next <= TimeSpan.Zero ? null : next;
    }

    private void AnnouncePauseRestart(TimeSpan remaining)
    {
        _chatManager.DispatchServerAnnouncement(
            Loc.GetString("server-updates-pause-restart-countdown", ("minutes", remaining.Minutes)));
    }
    // Moff end

    /// <summary>
    /// Notify that the round just ended, which is a great time to restart if necessary!
    /// </summary>
    /// <returns>True if the server is going to restart.</returns>
    public bool RoundEnded()
    {
        if (_updateOnRoundEnd || ShouldShutdownDueToUptime())
        {
            DoShutdown();
            return true;
        }

        return false;
    }

    private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        switch (e.NewStatus)
        {
            case SessionStatus.Connected:
                if (_restartTime != null)
                    _sawmill.Debug("Aborting server restart timer due to player connection");

                _restartTime = null;
                break;
            case SessionStatus.Disconnected:
                ServerEmptyUpdateRestartCheck("last player disconnect");
                break;
        }
    }

    private void WatchdogOnUpdateReceived()
    {
        _chatManager.DispatchServerAnnouncement(Loc.GetString("server-updates-received"));
        _updateOnRoundEnd = true;
        ServerEmptyUpdateRestartCheck("update notification");
    }

    /// <summary>
    ///     Checks whether there are still players on the server,
    /// and if not starts a timer to automatically reboot the server if an update is available.
    /// </summary>
    private void ServerEmptyUpdateRestartCheck(string reason)
    {
        // Can't simple check the current connected player count since that doesn't update
        // before PlayerStatusChanged gets fired.
        // So in the disconnect handler we'd still see a single player otherwise.
        var playersOnline = _playerManager.Sessions.Any(p => p.Status != SessionStatus.Disconnected);
        if (playersOnline || !(_updateOnRoundEnd || ShouldShutdownDueToUptime()))
        {
            // Still somebody online.
            return;
        }

        if (_restartTime != null)
        {
            // Do nothing because we already have a timer running.
            return;
        }

        var restartDelay = TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.UpdateRestartDelay));
        _restartTime = restartDelay + _gameTiming.RealTime;

        _sawmill.Debug("Started server-empty restart timer due to {Reason}", reason);
    }

    private void DoShutdown()
    {
        _sawmill.Debug($"Shutting down via {nameof(ServerUpdateManager)}!");
        var reason = _updateOnRoundEnd ? "server-updates-shutdown" : "server-updates-shutdown-uptime";
        _server.Shutdown(Loc.GetString(reason));
    }

    private bool ShouldShutdownDueToUptime()
    {
        return _uptimeRestart != TimeSpan.Zero && _gameTiming.RealTime > _uptimeRestart;
    }

    void IPostInjectInit.PostInject()
    {
        _sawmill = _logManager.GetSawmill("restart");
    }
}
