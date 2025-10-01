using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Bible.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Stunnable;
using Content.Server._Moffstation.Roles;
using Content.Shared.Trigger;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Mindshield.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles.Components;
using Content.Shared.NPC.Systems;
using Content.Shared._Moffstation.ClockCult.Components;
using Robust.Shared.Player;
// Debug Stuff that I'm too scared to touch
using Content.Server.Administration;
using Content.Shared.Mind.Components;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.CCVar;
using Content.Server.Chat.Managers;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server._Moffstation.ClockCult;

/// <summary>
/// Handles the conversion (and deconversion) of Clockwork Cultists
/// </summary>
public sealed class ClockworkCultConvertSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConvertOnTriggerComponent, TriggerEvent>(OnTriggerConvert);
    }

    /// <summary>
    /// Handles the converting of crew into cultists. Gotta add stuff so they can't convert zeds and the like.
    /// </summary>
    /// <param name="ent">What got triggered</param>
    /// <param name="uid">What triggered ent</param>
    private void OnTriggerConvert(Entity<ConvertOnTriggerComponent> ent, ref TriggerEvent uid)
    {

        if (uid.User == null) // Double Triple checks that uid.User isn't null
            return;
        EntityUid safeUser = uid.User.Value!;
        var isCultist = HasComp<ClockworkCultComponent>(safeUser);
        if (isCultist) // First checks if the target's a cultist
        {
            return;
        }


        var downTime = TimeSpan.FromSeconds(5);
        var stunTime = TimeSpan.FromSeconds(3);
        var isChaplain = HasComp<BibleUserComponent>(safeUser);
        var hasMindshield = HasComp<MindShieldComponent>(safeUser);

        //Stun and mute 'em since they're not a cultist
        _stunSystem.TryKnockdown(safeUser, downTime);
        _stunSystem.TryUpdateStunDuration(safeUser, stunTime);

        _adminLogManager.Add(LogType.Mind, LogImpact.Low, $"{ToPrettyString(safeUser)}; activated {ToPrettyString(ent)}");

        if (isChaplain || hasMindshield) // Then checks if the target is protected from conversion
        {
            return;
        }

        //Finally, checks if the target is being pulled by a cultist
        TryComp<PullableComponent>(safeUser, out var pullComp);
        if (pullComp == null) //Do they have the pullable component?
            return;
        if (pullComp.Puller == null) //Are they being pulled?
            return;
        EntityUid safePuller = pullComp.Puller.Value;
        if (!HasComp<ClockworkCultComponent>(safePuller)) //Checks if the puller is a cultist
            return;

        if (!_mind.TryGetMind(safeUser, out var mindId, out var mind))
            return;

        _npcFaction.AddFaction(safeUser, "ClockworkCult");

        var clockComp = EnsureComp<ClockworkCultComponent>(safeUser);
        _adminLogManager.Add(LogType.Mind,
            LogImpact.Medium,
            $"{ToPrettyString(safePuller)} converted {ToPrettyString(safeUser)} into a Clockwork Cultist"); //Tattles to the mods who converted who

        if (mindId == default || !_role.MindHasRole<ClockworkCultRoleComponent>(mindId))
        {
            _role.MindAddRole(mindId, "MindRoleClockworkCultist");
        }

        if (mind is { UserId: not null } && _player.TryGetSessionById(mind.UserId, out var session)) // Crap I don't understand
            _antag.SendBriefing(session, Loc.GetString("clockcult-briefing"), Color.SandyBrown, clockComp.ClockCultStartSound);

    }
}
