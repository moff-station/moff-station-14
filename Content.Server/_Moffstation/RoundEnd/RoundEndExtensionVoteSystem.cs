using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Server.Voting;
using Content.Server.Voting.Managers;
using Content.Shared._Moffstation.CCVar;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.RoundEnd;

/// <summary>
/// Pops up a vote shortly before post-round returns to the lobby, letting players extend it a limited number of times.
/// </summary>
public sealed class RoundEndExtensionVoteSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IVoteManager _voteManager = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;

    // Leaves the result a few seconds to be read before the return to lobby.
    private static readonly TimeSpan VoteBuffer = TimeSpan.FromSeconds(10);

    private int _extensions;
    private IVoteHandle? _vote;

    // The restart time the last vote was held for, so a failed vote isn't asked again.
    private TimeSpan? _votedRestartTime;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        if (_vote is { Finished: false, Cancelled: false })
            _vote.Cancel();

        _vote = null;
        _votedRestartTime = null;
        _extensions = 0;
    }

    public override void Update(float frameTime)
    {
        if (_gameTicker.RunLevel != GameRunLevel.PostRound
            || _roundEnd.RestartTime is not { } restartTime
            || restartTime == _votedRestartTime
            || _extensions >= _cfg.GetCVar(MoffCCVars.RoundEndExtensionVoteMax)
            || !_cfg.GetCVar(MoffCCVars.RoundEndExtensionVoteEnabled))
            return;

        var duration = TimeSpan.FromSeconds(_cfg.GetCVar(MoffCCVars.RoundEndExtensionVoteDuration));
        var remaining = restartTime - _timing.CurTime;

        // Too short a countdown to fit a vote in, e.g. deathmatch restarts.
        if (remaining <= duration)
        {
            _votedRestartTime = restartTime;
            return;
        }

        if (remaining > duration + VoteBuffer)
            return;

        _votedRestartTime = restartTime;
        StartVote(duration);
    }

    private void StartVote(TimeSpan duration)
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
        _vote = vote;
        vote.OnFinished += (_, _) => OnVoteFinished(vote, minutes);
    }

    private void OnVoteFinished(IVoteHandle vote, int minutes)
    {
        if (vote != _vote || _gameTicker.RunLevel != GameRunLevel.PostRound)
            return;

        var yes = vote.VotesPerOption[true];
        var no = vote.VotesPerOption[false];

        if (yes <= no)
        {
            _adminLogger.Add(LogType.Vote, LogImpact.Low, $"Round end extension vote failed: {yes}/{no}");
            _chat.DispatchServerAnnouncement(Loc.GetString("round-end-extension-vote-failed"));
            return;
        }

        _extensions++;
        _roundEnd.ExtendRestartCountdown(TimeSpan.FromMinutes(minutes));

        _adminLogger.Add(LogType.Vote, LogImpact.Low, $"Round end extension vote succeeded: {yes}/{no}");
        _chat.DispatchServerAnnouncement(Loc.GetString("round-end-extension-vote-succeeded",
            ("minutes", minutes),
            ("remaining", _cfg.GetCVar(MoffCCVars.RoundEndExtensionVoteMax) - _extensions)));
    }
}
