using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Moffstation.GameTicking.Rules;

public sealed partial class MoffAnnounceOnRuleStartedSystem : EntitySystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IChatManager _chatManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoffAnnounceOnRuleStartedComponent, GameRuleStartedEvent>(OnRuleStarted);
    }

    private void OnRuleStarted(Entity<MoffAnnounceOnRuleStartedComponent> ent, ref GameRuleStartedEvent args)
    {
        var message = Loc.GetString(ent.Comp.Message);

        if (ent.Comp.ServerAnnouncement)
        {
            _chatManager.DispatchServerAnnouncement(message, ent.Comp.Color);
            return;
        }

        var sender = ent.Comp.Sender != null ? Loc.GetString(ent.Comp.Sender) : null;
        _chat.DispatchGlobalAnnouncement(message, sender, announcementSound: ent.Comp.Sound, colorOverride: ent.Comp.Color);
    }
}
