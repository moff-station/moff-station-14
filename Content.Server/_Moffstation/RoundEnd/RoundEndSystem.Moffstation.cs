using System.Threading;
using Content.Server.Screens.Components;
using Content.Server.Voting;
using Content.Server.Voting.Managers;
using Content.Shared._Moffstation.CCVar;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.RoundEnd;

public sealed partial class RoundEndSystem
{
    [Dependency] private IVoteManager _voteManager = default!;

    private static readonly TimeSpan ExtensionVoteBuffer = TimeSpan.FromSeconds(10);

    private void ScheduleExtensionVote(TimeSpan countdown, int extensions)
    {
        var duration = TimeSpan.FromSeconds(_cfg.GetCVar(MoffCCVars.RoundEndExtensionVoteDuration));

        if (_countdownTokenSource == null
            || extensions >= _cfg.GetCVar(MoffCCVars.MaxRoundEndExtensionVotes)
            || countdown <= duration)
            return;

        var token = _countdownTokenSource.Token;
        var restartTime = _gameTiming.CurTime + countdown;
        var delay = countdown - duration - ExtensionVoteBuffer;
        Timer.Spawn(delay > TimeSpan.Zero ? delay : TimeSpan.Zero,
            () => StartExtensionVote(restartTime, duration, extensions, token),
            token);
    }

    // Unfortunately votes are lowkirk slop.
    // If you don't make a preset vote you don't get many of the bells and whistles automatically.
    // In my infinite wisdom, I think it will be easier to maintain to create most of those bells and whistles here, rather than make a preset vote type.
    // The preset vote type involves sticking your fingers into alot of the upstream vote files, which could make merge conflicts a pain
    // Thank you Cent for coming to my ted talk, if you are reading this I have stashed the password to the server under your doormat alongside 20 portuguese dollars, in case I meet my unfortunate demise.
    // tldr, I'm putting this here instead of the upstream vote file because it's easier.
    private void StartExtensionVote(TimeSpan restartTime, TimeSpan duration, int extensions, CancellationToken token)
    {
        var minutes = _cfg.GetCVar(MoffCCVars.RoundEndExtensionVoteMinutes);
        var options = new VoteOptions
        {
            Title = Loc.GetString("round-end-extension-vote-title", ("minutes", minutes)),
            Options =
            {
                (Loc.GetString("round-end-extension-vote-yes"), true),
                (Loc.GetString("round-end-extension-vote-no"), false),
            },
            Duration = duration,
        };
        options.SetInitiatorOrServer(null);

        var vote = _voteManager.CreateVote(options);
        vote.OnFinished += (_, _) =>
        {
            if (token.IsCancellationRequested)
                return;

            var yes = vote.VotesPerOption[true];
            var no = vote.VotesPerOption[false];

            if (yes <= no)
            {
                _adminLogger.Add(LogType.Vote, LogImpact.Low, $"Round end extension vote failed: {yes}/{no}");
                return;
            }

            _countdownTokenSource?.Cancel();
            _countdownTokenSource = new CancellationTokenSource();
            var countdown = restartTime + TimeSpan.FromMinutes(minutes) - _gameTiming.CurTime;
            Timer.Spawn(countdown, AfterEndRoundRestart, _countdownTokenSource.Token);
            UpdateRestartScreens(countdown);

            _adminLogger.Add(LogType.Vote, LogImpact.Low, $"Round end extension vote succeeded: {yes}/{no}");
            _chatManager.DispatchServerAnnouncement(Loc.GetString("round-end-extension-vote-succeeded",
                ("minutes", minutes),
                ("remaining", _cfg.GetCVar(MoffCCVars.MaxRoundEndExtensionVotes) - extensions - 1)));

            ScheduleExtensionVote(countdown, extensions + 1);
        };
    }

    private void UpdateRestartScreens(TimeSpan countdown)
    {
        if (_shuttle.GetShuttle() is not { } shuttle || !TryComp<DeviceNetworkComponent>(shuttle, out var net))
            return;

        var payload = new NetworkPayload
        {
            [ShuttleTimerMasks.ShuttleMap] = shuttle,
            [ShuttleTimerMasks.SourceMap] = GetCentcomm(),
            [ShuttleTimerMasks.DestMap] = GetStation(),
            [ShuttleTimerMasks.ShuttleTime] = countdown,
            [ShuttleTimerMasks.SourceTime] = countdown,
            [ShuttleTimerMasks.DestTime] = countdown,
            [ScreenMasks.Text] = ShuttleTimerMasks.Bye,
        };
        _deviceNetworkSystem.QueuePacket(shuttle, null, payload, net.TransmitFrequency);
    }
}
