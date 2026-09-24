using Content.Server.GameTicking;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.RoundEnd;

public sealed partial class RoundEndSystem
{
    /// <summary>
    /// When the post-round countdown will return to the lobby, or null outside of post-round.
    /// </summary>
    public TimeSpan? RestartTime { get; private set; }

    /// <summary>
    /// Pushes the post-round return to lobby back by the given time.
    /// </summary>
    public void ExtendRestartCountdown(TimeSpan extension)
    {
        if (_gameTicker.RunLevel != GameRunLevel.PostRound || RestartTime == null)
            return;

        _countdownTokenSource?.Cancel();
        _countdownTokenSource = new();

        RestartTime += extension;
        Timer.Spawn(RestartTime.Value - _gameTiming.CurTime, AfterEndRoundRestart, _countdownTokenSource.Token);
    }
}
