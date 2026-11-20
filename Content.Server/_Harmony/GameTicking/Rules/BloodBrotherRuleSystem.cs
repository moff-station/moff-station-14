using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._Harmony.BloodBrothers.EntitySystems;
using Content.Server._Harmony.GameTicking.Rules.Components;
using Content.Server._Harmony.Roles;
using Content.Server._Moffstation.Station;
using Content.Shared._Moffstation.Extensions;
using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Popups;
using Content.Server.Preferences.Managers;
using Content.Server.Roles;
using Content.Server.Stunnable;
using Content.Shared._Harmony.BloodBrothers.Components;
using Content.Shared.Antag;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mindshield;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Codewords;
using Content.Shared.Zombies;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Harmony.GameTicking.Rules;

public sealed partial class BloodBrotherRuleSystem : GameRuleSystem<BloodBrotherRuleComponent>
{
    [Dependency] private IAdminLogManager _adminLogManager = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IServerPreferencesManager _preferencesManager = default!;
    [Dependency] private AntagSelectionSystem _antagSystem = default!;
    [Dependency] private MindShieldSystem _mindShield = default!;
    [Dependency] private MindSystem _mindSystem = default!;
    [Dependency] private MobStateSystem _mobStateSystem = default!;
    [Dependency] private NpcFactionSystem _npcFactionSystem = default!;
    [Dependency] private ObjectivesSystem _objectivesSystem = default!;
    [Dependency] private PopupSystem _popupSystem = default!;
    [Dependency] private RoleSystem _roleSystem = default!;
    [Dependency] private StunSystem _stunSystem = default!;
    [Dependency] private TargetObjectiveSystem _targetObjectiveSystem = default!;
    [Dependency] private ServerBloodBrotherSystem _bloodBrother = default!;
    [Dependency] private MoffCharacterPickerSystem _characterPicker = default!;

    [Dependency] private EntityQuery<BloodBrotherComponent> _bloodBrotherQuery;
    [Dependency] private EntityQuery<TargetObjectiveComponent> _targetObjectiveQuery;
    [Dependency] private EntityQuery<ZombieComponent> _zombieQuery;

    private static readonly ProtoId<RoleTypePrototype>[] ImmuneRoleTypes =
        {"SoloAntagonist", "TeamAntagonist"}; // Traitors and Nukies and other team antags

    private static readonly ProtoId<AntagPrototype> RequiredAntagPreference = "BloodBrotherConvertible";

    private static readonly LocId FailedNoMind = "blood-brother-convert-failed-no-mind";
    private static readonly LocId FailedAlreadyBrother = "blood-brother-convert-failed-already-brother";
    private static readonly LocId FailedHasBrother = "blood-brother-convert-failed-has-brother";
    private static readonly LocId FailedTarget = "blood-brother-convert-failed-target";
    private static readonly LocId FailedZombie = "blood-brother-convert-failed-zombie";
    private static readonly LocId FailedShielded = "blood-brother-convert-failed-shielded";
    private static readonly LocId FailedDead = "blood-brother-convert-failed-dead";
    private static readonly LocId FailedPreference = "blood-brother-convert-failed-preference";

    protected override void ActiveTick(EntityUid uid,
        BloodBrotherRuleComponent component,
        GameRuleComponent gameRule,
        float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (Timing.CurTime < component.NextConvertibleUpdate)
            return;

        component.NextConvertibleUpdate = Timing.CurTime + component.ConvertibleUpdateInterval;

        // Check if any blood brothers can even convert in the first place.
        // If they can't then don't bother running the expensive ass check on everyone in the round
        var anyCanConvert = false;
        foreach (var initial in EntityQueryEnumerator<InitialBloodBrotherComponent>())
        {
            if (!CanConvert(initial.AsNullable(), out _))
                continue;

            anyCanConvert = true;
            break;
        }

        if (!anyCanConvert)
        {
            _bloodBrother.ClearConvertible();
            return;
        }

        // This checks over every player in the round to see if they can be converted.
        foreach (var target in EntityQueryEnumerator<HumanoidProfileComponent>())
        {
            _bloodBrother.SetConvertible(target, CanBeConverted(target, out _));
        }
    }

    protected override void Ended(Entity<BloodBrotherRuleComponent> rule, ref GameRuleEndedEvent args)
    {
        base.Ended(rule, ref args);

        _bloodBrother.ClearConvertible();
    }

    [SubscribeLocalEvent]
    private void OnObjectivesTextPrepend(Entity<BloodBrotherRuleComponent> entity, ref ObjectivesTextPrependEvent args)
    {
        var antags = _antagSystem.GetAntagIdentifiers(entity.Owner);

        foreach (var (mind, sessionData, name) in antags)
        {
            if (!_roleSystem.MindHasRole<BloodBrotherRoleComponent>(mind, out var role))
                continue;

            var brotherRole = role.Value.Comp2;

            if (brotherRole.Brother == null)
                continue;

            if (!_mindSystem.TryGetMind(brotherRole.Brother.Value, out _, out var brotherMind)
                || brotherMind.UserId == null)
            {
                args.Text += "\n" + Loc.GetString("blood-brother-round-end-no-mind",
                    ("name", name),
                    ("username", sessionData.UserName),
                    ("brotherName", MetaData(role.Value).EntityName));

                continue;
            }

            var brotherUsername = _playerManager.GetPlayerData(brotherMind.UserId.Value).UserName;

            args.Text += "\n" + Loc.GetString("blood-brother-round-end",
                ("name", name),
                ("username", sessionData.UserName),
                ("brotherName", MetaData(brotherRole.Brother.Value).EntityName),
                ("brotherUsername", (brotherUsername)));
        }
    }

    /// <summary>
    /// Converts someone to blood bound
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBloodBrotherConvert(Entity<InitialBloodBrotherComponent> entity,
        ref BloodBrotherConvertActionEvent args)
    {
        // Check if convertible
        if (!TryComp<BloodBrotherComponent>(entity, out var originalComponent))
            return;

        if (!CanConvert(entity.AsNullable(), args.Target, out var failureMessage))
        {
            _popupSystem.PopupEntity(
                Loc.GetString(failureMessage,
                    ("converter", Identity.Entity(entity, _entityManager)),
                    ("converted", Identity.Entity(args.Target, _entityManager))),
                args.Target,
                entity,
                PopupType.MediumCaution);
            return;
        }

        if (!_mindSystem.TryGetMind(entity, out var mindId, out _))
            return;

        if (!_mindSystem.TryGetMind(args.Target, out var targetMindId, out var targetMind))
            return;

        // Actual conversion logic
        var convertedComp = CopyComp(entity, args.Target, originalComponent);

        _npcFactionSystem.AddFaction(args.Target, entity.Comp.BloodBrotherFaction);

        _adminLogManager.Add(LogType.Mind,
            LogImpact.Medium,
            $"{ToPrettyString(entity)} converted {ToPrettyString(args.Target)} into their Blood Brother");

        originalComponent.Brother = args.Target;
        if (_roleSystem.MindHasRole<BloodBrotherRoleComponent>(mindId, out var role))
            role.Value.Comp2.Brother = args.Target;

        if (!_roleSystem.MindHasRole(targetMindId, out Entity<MindRoleComponent, BloodBrotherRoleComponent>? targetRole))
        {
            _roleSystem.MindAddRole(targetMindId, entity.Comp.BloodBrotherMindRole, targetMind);
            _roleSystem.MindHasRole(targetMindId, out targetRole);
        }

        DebugTools.AssertNotNull(targetRole, "Blood brother role was null after assigning it.");

        convertedComp.Brother = entity;
        targetRole!.Value.Comp2.Brother = entity;

        if (!_objectivesSystem.TryCreateObjective((targetMindId, targetMind),
                entity.Comp.ConvertedBrotherObjective,
                out var newObjective))
            return;

        var targetObjective = EnsureComp<TargetObjectiveComponent>(newObjective.Value);

        _targetObjectiveSystem.SetTarget(newObjective.Value, mindId, targetObjective);

        _mindSystem.AddObjective(targetMindId, targetMind, newObjective.Value);

        // Visuals
        _antagSystem.SendBriefing(args.Target,
            Loc.GetString(entity.Comp.BriefingText),
            entity.Comp.BriefingColor,
            entity.Comp.BriefingSound);

        _popupSystem.PopupEntity(
            Loc.GetString(
                entity.Comp.ConvertPopupText,
                ("converter", Identity.Entity(entity, _entityManager)),
                ("converted", Identity.Entity(args.Target, _entityManager))),
            args.Target,
            PopupType.LargeCaution);

        if (entity.Comp.ConvertStunTime != null)
            _stunSystem.TryAddParalyzeDuration(args.Target, entity.Comp.ConvertStunTime.Value);

        // Cleanup the data
        RemCompDeferred<InitialBloodBrotherComponent>(entity);

        Dirty(entity, originalComponent);
        Dirty(args.Target, convertedComp);
    }

    /// Checks whether a blood bound is capable of converting anyone at all.
    public bool CanConvert(
        Entity<InitialBloodBrotherComponent?> entity,
        [NotNullWhen(false)] out string? errorMessage)
    {
        errorMessage = null;

        if (!Resolve(entity.Owner, ref entity.Comp) || !_mindSystem.TryGetMind(entity.Owner, out _, out _))
        {
            errorMessage = FailedNoMind;
            return false;
        }

        if (!_bloodBrotherQuery.TryComp(entity.Owner, out var converterBBComp) || converterBBComp.Brother != null)
        {
            Log.Error($"{ToPrettyString(entity.Owner)} tried to convert but was missing a {nameof(BloodBrotherComponent)} or already had a brother.");
            errorMessage = FailedHasBrother;
            return false;
        }

        return true;
    }

    /// Checks whether a blood bound can convert a specific target.
    public bool CanConvert(
        Entity<InitialBloodBrotherComponent?> entity,
        EntityUid target,
        [NotNullWhen(false)] out string? errorMessage)
    {
        if (!CanConvert(entity, out errorMessage) || !CanBeConverted(target, out errorMessage))
            return false;

        if (_mindSystem.TryGetMind(entity.Owner, out _, out var converterMind)
            && _mindSystem.TryGetMind(target, out var targetMindId, out _)
            && GetObjectiveTargets(converterMind).Contains(targetMindId))
        {
            errorMessage = FailedTarget;
            return false;
        }

        return true;
    }

    /// Checks whether a target can be converted to a blood bound.
    private bool CanBeConverted(
        EntityUid target,
        [NotNullWhen(false)] out string? errorMessage)
    {
        errorMessage = null;

        if (_bloodBrotherQuery.HasComp(target))
        {
            errorMessage = FailedAlreadyBrother;
            return false;
        }

        if (_zombieQuery.HasComp(target))
        {
            errorMessage = FailedZombie;
            return false;
        }

        if (!_mobStateSystem.IsAlive(target))
        {
            errorMessage = FailedDead;
            return false;
        }

        if (!_mindSystem.TryGetMind(target, out _, out var targetMind) || targetMind.UserId is not { } userId)
        {
            errorMessage = FailedNoMind;
            return false;
        }

        _mindShield.GetMindshieldStatus(target, out var targetIsMindshielded, out _);
        if (targetIsMindshielded)
        {
            errorMessage = FailedShielded;
            return false;
        }

        if (ImmuneRoleTypes.Contains(targetMind.RoleType) || !HasAntagPreference(userId))
        {
            errorMessage = FailedPreference;
            return false;
        }

        return true;
    }

    private bool HasAntagPreference(NetUserId userId)
    {
        var profile = _characterPicker.GetSpawnedProfile(userId);
        if (profile == null && _preferencesManager.TryGetCachedPreferences(userId, out var preferences))
            profile = preferences.SelectedCharacter;

        return profile != null && profile.AntagPreferences.Contains(RequiredAntagPreference);
    }

    private IEnumerable<EntityUid> GetObjectiveTargets(MindComponent mind) => _targetObjectiveQuery
        .ResolveAll(mind.Objectives, logMissing: false)
        .Select(it => it.Comp.Target)
        .OfType<EntityUid>();
}
