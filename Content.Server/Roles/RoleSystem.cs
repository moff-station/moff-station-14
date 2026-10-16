using Content.Server.Chat.Managers;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Network;

namespace Content.Server.Roles;

public sealed partial class RoleSystem : SharedRoleSystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private IChatManager _chat = default!;

    public string? MindGetBriefing(EntityUid? mindId)
    {
        if (mindId == null)
        {
            Log.Error($"MindGetBriefing failed for mind {mindId}");
            return null;
        }

        TryComp<MindComponent>(mindId.Value, out var mindComp);

        if (mindComp is null)
        {
            Log.Error($"MindGetBriefing failed for mind {mindId}");
            return null;
        }

        // Moffstation - Start - only show the latest mind role's briefing
        string? latestBriefing = null;
        foreach (var role in mindComp.MindRoleContainer.ContainedEntities)
        {
            var ev = new GetBriefingEvent();
            ev.Mind = (mindId.Value, mindComp);
            RaiseLocalEvent(role, ref ev);
            if (ev.Briefing != null)
                latestBriefing = ev.Briefing;
        }

        return latestBriefing;
        // Moffstation - End
    }

    public void RoleUpdateMessage(MindComponent mind)
    {
        if (!Player.TryGetSessionById(mind.UserId, out var session))
            return;

        if (!ProtoMan.Resolve(mind.RoleType, out var proto))
            return;

        var roleText = Loc.GetString(proto.Name);
        var color = proto.Color;

        //TODO add audio? Would need to be optional so it does not play on role changes that already come with their own audio
        // _audio.PlayGlobal(Sound, session);

        var message = Loc.GetString("role-type-update-message", ("color", color), ("role", roleText));
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        _chat.ChatMessageToOne(ChatChannel.Server,
            message,
            wrappedMessage,
            default,
            false,
            session.Channel);
    }

    protected override void UpdateCharacterWindow(NetUserId? user, MindStringRepresentation mindString)
    {
        if (Player.TryGetSessionById(user, out var session))
        {
            RaiseNetworkEvent(new MindRoleTypeChangedEvent(), session.Channel);
        }
        else
        {
            _adminLogger.Add(
                LogType.Mind,
                LogImpact.Medium,
                $"The Character Window of {mindString} potentially did not update immediately : session error");
        }
    }
}
