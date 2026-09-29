using System.Threading;
using Content.Server.RoundEnd.Components;
using Content.Server.Voting;
using Content.Server.Voting.Managers;
using Content.Shared._Moffstation.CCVar;
using Content.Shared.Database;
using Robust.Shared.Map;

namespace Content.Server.RoundEnd;

public sealed partial class RoundEndSystem
{
    [Dependency] private IVoteManager _voteManager = default!;

    private static readonly TimeSpan ExtensionVoteBuffer = TimeSpan.FromSeconds(10);

    //recursion but gay
    private void ScheduleExtensionVote(TimeSpan countdown, int extensions)
    {
        var duration = TimeSpan.FromSeconds(_cfg.GetCVar(MoffCCVars.RoundEndExtensionVoteDuration));

        if (_countdownTokenSource == null
            || extensions >= _cfg.GetCVar(MoffCCVars.MaxRoundEndExtensionVotes)
            || countdown <= duration)
            return;

        var delay = countdown - duration - ExtensionVoteBuffer;
        var vote = AddComp<RoundEndExtensionVoteComponent>(Spawn(null, MapCoordinates.Nullspace));
        vote.StartAt = _gameTiming.CurTime + (delay > TimeSpan.Zero ? delay : TimeSpan.Zero);
        vote.RestartTime = _gameTiming.CurTime + countdown;
        vote.Duration = duration;
        vote.Extensions = extensions;
        vote.Token = _countdownTokenSource.Token;
    }

    private void UpdateRoundEndExtensions()
    {
        foreach (var ent in EntityQueryEnumerator<RoundEndExtensionVoteComponent>())
        {
            if (ent.Comp.Token.IsCancellationRequested)
            {
                QueueDel(ent.Owner);
                continue;
            }

            if (_gameTiming.CurTime < ent.Comp.StartAt)
                continue;

            StartExtensionVote(ent.Comp.RestartTime, ent.Comp.Duration, ent.Comp.Extensions, ent.Comp.Token);
            QueueDel(ent.Owner);
        }

        var restart = false;
        foreach (var ent in EntityQueryEnumerator<RoundEndExtensionRestartComponent>())
        {
            if (ent.Comp.Token.IsCancellationRequested)
            {
                QueueDel(ent.Owner);
                continue;
            }

            if (_gameTiming.CurTime < ent.Comp.RestartAt)
                continue;

            QueueDel(ent.Owner);
            restart = true;
        }

        // Restarting deletes every entity, so it can't happen mid-query.
        if (restart)
            AfterEndRoundRestart();
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
                _adminLogger.Add(LogType.Vote, LogImpact.Low, $"Round end extension vote failed: y={yes}/n={no}");
                return;
            }

            _countdownTokenSource?.Cancel();
            _countdownTokenSource = new CancellationTokenSource();
            var countdown = restartTime + TimeSpan.FromMinutes(minutes) - _gameTiming.CurTime;
            var restart = AddComp<RoundEndExtensionRestartComponent>(Spawn(null, MapCoordinates.Nullspace));
            restart.RestartAt = _gameTiming.CurTime + countdown;
            restart.Token = _countdownTokenSource.Token;
            if (_shuttle.GetShuttle() is { } shuttle)
                _shuttle.UpdateRoundEndScreens(shuttle, countdown);

            _adminLogger.Add(LogType.Vote, LogImpact.Low, $"Round end extension vote succeeded: y={yes}/n={no}");
            _chatManager.DispatchServerAnnouncement(Loc.GetString("round-end-extension-vote-succeeded",
                ("minutes", minutes)));

            ScheduleExtensionVote(countdown, extensions + 1);
        };
    }
}
