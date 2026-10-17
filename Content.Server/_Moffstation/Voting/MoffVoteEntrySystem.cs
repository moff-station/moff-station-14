using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Shared._ES.Voting.Components;
using Content.Shared._Moffstation.Voting.Components;
using Content.Shared._Moffstation.Voting.Systems;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.GameWindow;
using Robust.Shared.Player;

namespace Content.Server._Moffstation.Voting;

public sealed partial class MoffVoteEntrySystem : MoffSharedVoteEntrySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IAdminManager _adminManager = default!;

    private const string VoteSound = "/Audio/Effects/voteding.ogg";

    protected override void SendVoteStartAnnouncement(
        Entity<MoffVoteEntryComponent> ent,
        Entity<ESVoterComponent, ActorComponent> voter)
    {
        var session = voter.Comp2.PlayerSession;

        var msg = Loc.GetString("es-voter-chat-announce-result",
            ("query", Loc.GetString("es-voter-chat-announce-vote-start")),
            ("result", Name(ent)));
        var wrappedMsg = Loc.GetString("es-voter-chat-announce-wrap-message", ("message", msg));
        _chat.ChatMessageToOne(ChatChannel.Server,
            msg,
            wrappedMsg,
            ent,
            false,
            session.Channel,
            Color.Plum,
            audioPath: voter.Comp1.NotificationsEnabled ? VoteSound : null);

        // Don't notify admins, they are not taking the roles and it can get mixed up with ahelps
        if (voter.Comp1.NotificationsEnabled && !_adminManager.IsAdmin(session))
            RaiseNetworkEvent(new RequestWindowAttentionEvent(), session);
    }

    protected override void OnVoteStart(Entity<MoffVoteEntryComponent> ent)
    {
        _adminLog.Add(LogType.Vote, LogImpact.Medium, $"Started vote for {ToPrettyString(ent)}.");
    }
}
