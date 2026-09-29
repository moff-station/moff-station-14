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

    private TimeSpan ExtensionVoteDuration => TimeSpan.FromSeconds(_cfg.GetCVar(MoffCCVars.RoundEndExtensionVoteDuration));

    private void InitializeRoundEndExtension(TimeSpan countdown)
    {
        var manager = Spawn();
        var comp = AddComp<RoundEndExtensionManagerComponent>(manager);
        comp.RestartAt = _gameTiming.CurTime + countdown;
        ScheduleExtensionVote((manager, comp));
    }

    // recursion but gay
    /// <summary>
    /// Does the setup for the next extension event, if the previous one passed.
    /// Short circuits if the limit of extensions has been reached, or if a vote would take too long before the restart.
    /// </summary>
    private void ScheduleExtensionVote(Entity<RoundEndExtensionManagerComponent> ent)
    {
        var duration = ExtensionVoteDuration;
        var countdown = ent.Comp.RestartAt - _gameTiming.CurTime;

        if (ent.Comp.Extensions >= _cfg.GetCVar(MoffCCVars.MaxRoundEndExtensionVotes)
            || countdown <= duration)
            return;

        var delay = MathHelper.Max(countdown - duration - ExtensionVoteBuffer, TimeSpan.Zero);
        ent.Comp.NextVote = _gameTiming.CurTime + (delay > TimeSpan.Zero ? delay : TimeSpan.Zero);
    }

    // Called every update tick
    private void RoundEndExtensionsUpdate()
    {
        var restart = false;
        foreach (var ent in EntityQueryEnumerator<RoundEndExtensionManagerComponent>())
        {
            if (ent.Comp.NextVote is { } nextVote && _gameTiming.CurTime >= nextVote)
            {
                ent.Comp.NextVote = null;
                StartExtensionVote(ent);
            }

            if (_gameTiming.CurTime >= ent.Comp.RestartAt)
            {
                QueueDel(ent.Owner);
                restart = true;
            }
        }

        if (restart)
            AfterEndRoundRestart();
    }

    // Unfortunately votes are lowkirk slop.
    // If you don't make a preset vote you don't get many of the bells and whistles automatically.
    // In my infinite wisdom, I think it will be easier to maintain to create most of those bells and whistles here, rather than make a preset vote type.
    // The preset vote type involves sticking your fingers into alot of the upstream vote files, which could make merge conflicts a pain
    // Thank you Cent for coming to my ted talk, if you are reading this I have stashed the password to the server under your doormat alongside 20 portuguese dollars, in case I meet my unfortunate demise.
    // tldr, I'm putting this here instead of the upstream vote file because it's easier.
    private void StartExtensionVote(Entity<RoundEndExtensionManagerComponent> ent)
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
            Duration = ExtensionVoteDuration,
        };
        options.SetInitiatorOrServer(null);

        var vote = _voteManager.CreateVote(options);
        vote.OnFinished += (_, _) =>
        {
            var yes = vote.VotesPerOption[true];
            var no = vote.VotesPerOption[false];

            if (yes <= no)
            {
                _adminLogger.Add(LogType.Vote, LogImpact.Low, $"Round end extension vote failed: y={yes}/n={no}");
                return;
            }

            _countdownTokenSource?.Cancel();
            _countdownTokenSource = new CancellationTokenSource();
            ent.Comp.Extensions++;
            ent.Comp.RestartAt += TimeSpan.FromMinutes(minutes);
            if (_shuttle.GetShuttle() is { } shuttle)
                _shuttle.UpdateRoundEndScreens(shuttle, ent.Comp.RestartAt - _gameTiming.CurTime);

            _adminLogger.Add(LogType.Vote, LogImpact.Low, $"Round end extension vote succeeded: y={yes}/n={no}");
            _chatManager.DispatchServerAnnouncement(Loc.GetString("round-end-extension-vote-succeeded",
                ("minutes", minutes)));

            ScheduleExtensionVote(ent);
        };
    }
}
