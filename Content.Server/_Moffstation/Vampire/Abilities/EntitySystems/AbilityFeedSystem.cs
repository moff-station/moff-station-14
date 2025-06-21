using Content.Server._Moffstation.Vampire.EntitySystems;
using Content.Server.Body.Components;
using Content.Shared._Moffstation.Vampire.Abilities.Components;
using Content.Shared._Moffstation.Vampire.Components;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared._Moffstation.Vampire.Events;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Database;
using NetCord;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Moffstation.Vampire.Abilities.EntitySystems;

/// <summary>
/// This system handles the Feed ability with the
/// <see cref="Content.Shared._Moffstation.Vampire.Abilities.Components.AbilityFeedComponent"/>
/// which is used by vampires (or possibly other blood-sucking creatures) to extract blood and BloodEssence from
/// another creature.
/// </summary>
public sealed class AbilityFeedSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly BloodEssenceUserSystem _bloodEssence = default!;
    [Dependency] private readonly VampireSystem _vampire = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbilityFeedComponent, VampireEventFeedAbility>(OnFeedStart);
        SubscribeLocalEvent<AbilityFeedComponent, VampireEventFeedAbilityDoAfter>(OnFeedEnd);
        SubscribeLocalEvent<AbilityFeedComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<AbilityFeedComponent> entity, ref MapInitEvent args)
    {
        _action.AddAction(entity, ref entity.Comp.Action, entity.Comp.ActionProto, entity);
    }

    /// <summary>
    /// Triggered when the feed action is used on an entity.
    /// This sets up the do-after and starts it while also playing a sound and performing pop ups.
    /// The do-after triggers <see cref="OnFeedEnd"/>
    /// </summary>
    private void OnFeedStart(Entity<AbilityFeedComponent> entity, ref VampireEventFeedAbility args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (args.Target == args.Performer)
            return;

        var target = args.Target;

        if (!HasComp<BloodstreamComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-target-has-no-bloodstream", ("target", target)),
                entity,
                entity,
                PopupType.Medium);
            return;
        }

        var feedDoAfter = new DoAfterArgs(EntityManager, entity, entity.Comp.FeedDuration, new VampireEventFeedAbilityDoAfter(), entity, target: target)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            BreakOnDamage = true,
        };

        if (!_doAfter.TryStartDoAfter(feedDoAfter))
            return;

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(entity):user} started to feed on {ToPrettyString(target):user}.");

        _audio.PlayPvs(entity.Comp.FeedStartSound, entity);
        _popup.PopupEntity(Loc.GetString("vampire-feeding-on-vampire", ("target", target)), entity, entity, PopupType.Medium);
        _popup.PopupEntity(Loc.GetString("vampire-feeding-on-target", ("vampire", entity)), entity, target, PopupType.LargeCaution);
    }

    /// <summary>
    /// Triggered by the do-after started by <see cref="OnFeedStart"/>.
    /// Checks if the user has all the proper components, and con
    /// </summary>
    private void OnFeedEnd(Entity<AbilityFeedComponent> entity, ref VampireEventFeedAbilityDoAfter args)
    {
        // if canceled return (maybe spray blood?)
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;

        if (!TryComp<VampireComponent>(entity, out var vampire)
            || !TryComp<BloodEssenceUserComponent>(entity, out var bloodEssenceUser)
            || !TryComp<BodyComponent>(entity, out var body))
            return;

        var target = args.Args.Target;
        if (target is not { } || !TryComp<BloodstreamComponent>(target, out var targetBloodstream))
            return;

        var collectedEssence = _bloodEssence.TryExtractBlood((entity.Owner, bloodEssenceUser, body), entity.Comp.BloodPerFeed, (target.Value, targetBloodstream));
        if (collectedEssence > 0.0f)
        {
            _popup.PopupEntity(Loc.GetString("vampire-feeding-successful-vampire", ("target", target)), entity, entity, PopupType.Medium);
            _popup.PopupEntity(Loc.GetString("vampire-feeding-successful-target", ("vampire", entity.Owner)), entity.Owner, target.Value, PopupType.MediumCaution);
            _vampire.DepositEssence((entity, vampire), collectedEssence);
        }

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(entity):user} finished feeding on {ToPrettyString(target):user} and collected {collectedEssence} BloodEssence.");
        _audio.PlayPvs(entity.Comp.FeedSuccessSound, entity);
    }
}
