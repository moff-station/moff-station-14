using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Shared._ES.Voting;
using Content.Shared._ES.Voting.Components;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;

namespace Content.Server._ES.Voting;

/// <inheritdoc/>
public sealed class ESVoteSystem : ESSharedVoteSystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IChatManager _chat = default!;

    private const string VoteSound = "/Audio/Effects/voteding.ogg";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRuleComponent, ESSynchronizedVotesPostCompletedEvent>(OnPostCompleted);
    }

    private void OnPostCompleted(Entity<GameRuleComponent> ent, ref ESSynchronizedVotesPostCompletedEvent args)
    {
        // We manually start this rule now that the votes have concluded.
        // Is this kinda hacky? yes. I don't think it's that bad though
        Comp<GameRuleComponent>(ent).Added = true;
        var ev = new GameRuleAddedEvent(ent, Prototype(ent)!.ID);
        RaiseLocalEvent(ent, ref ev, true);
    }

    protected override void SendVoteResultAnnouncement(Entity<ESVoteComponent> ent, ESVoteOption result)
    {
        var voters = new List<INetChannel>();
        var query = EntityQueryEnumerator<ESVoterComponent, ActorComponent>();
        while (query.MoveNext(out _, out _, out var actor))
        {
            voters.Add(actor.PlayerSession.Channel);
        }

        var msg = Loc.GetString("es-voter-chat-announce-result",
            ("query", Loc.GetString(ent.Comp.QueryString)),
            ("result", result.DisplayString));
        var wrappedMsg = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chat.ChatMessageToMany(ChatChannel.Server, msg, wrappedMsg, ent, false, true, voters, Color.Plum, audioPath: VoteSound);
        _adminLog.Add(LogType.Vote, LogImpact.Medium, $"Finished vote for {ToPrettyString(ent)}. Vote conclusion: \"{msg}\"");
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class ESVoteCommand : ToolshedCommand
{
    [CommandImplementation("ls")]
    public IEnumerable<Entity<ESVoteComponent>> List()
    {
        var query = EntityManager.EntityQueryEnumerator<ESVoteComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            yield return (uid, comp);
        }
    }

    [CommandImplementation("options")]
    public IEnumerable<string> Options([PipedArgument] Entity<ESVoteComponent> vote)
    {
        foreach (var option in vote.Comp.VoteOptions)
        {
            yield return option.DisplayString;
        }
    }

    [CommandImplementation("tally")]
    public IEnumerable<string> Tally([PipedArgument] Entity<ESVoteComponent> vote)
    {
        foreach (var (option, votes) in vote.Comp.Votes)
        {
            yield return $"{option.DisplayString}: {votes.Count}";
        }
    }

    [CommandImplementation("end")]
    public void End([PipedArgument] Entity<ESVoteComponent> vote)
    {
        var sys = Sys<ESVoteSystem>();
        sys.EndVote(vote);
    }
}
