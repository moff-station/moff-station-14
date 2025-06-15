using Content.Server._Moffstation.Vampire.Abilities.Components;
using Content.Server._Moffstation.Vampire.EntitySystems;
using Content.Server.Body.Components;
using Content.Shared._Moffstation.Vampire;
using Content.Shared._Moffstation.Vampire.Components;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared._Moffstation.Vampire.Events;
using Content.Shared.Actions;
using Content.Shared.FixedPoint;

namespace Content.Server._Moffstation.Vampire.Abilities.EntitySystems;

public sealed class AbilityFeedSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly BloodEssenceUserSystem _bloodEssence = default!;
    [Dependency] private readonly VampireSystem _vampire = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbilityFeedComponent, VampireEventFeedAbility>(OnFeedStart);
        SubscribeLocalEvent<AbilityFeedComponent, VampireEventFeedAbilityDoAfter>(OnFeedEnd);
        SubscribeLocalEvent<AbilityFeedComponent, MapInitEvent>(OnMapInit);
    }

    public void OnMapInit(EntityUid uid, AbilityFeedComponent? comp, MapInitEvent args)
    {
        if (!Resolve(uid, ref comp))
            return;
        _action.AddAction(uid, ref comp.Action, comp.ActionProto, uid);
    }

    private void OnFeedStart(EntityUid uid, AbilityFeedComponent? component, VampireEventFeedAbility args)
    {
        if (args.Handled)
            return;

        if (!Resolve(uid, ref component))
            return;

        args.Handled = true;

        if (args.Target == args.Performer)
            return;

        var target = args.Target;

        if (!HasComp<BloodstreamComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-target-has-no-bloodstream", ("target", target)),
                uid,
                uid,
                PopupType.Medium);
            return;
        }

        var feedDoAfter = new DoAfterArgs(EntityManager, uid, component.FeedDuration, new VampireEventFeedAbilityDoAfter(), uid, target: target)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            BreakOnDamage = true,
        };

        if (!_doAfter.TryStartDoAfter(feedDoAfter))
            return;

        _popup.PopupEntity(Loc.GetString("vampire-feeding-on-vampire", ("target", target)), uid, uid, PopupType.Medium);
        _popup.PopupEntity(Loc.GetString("vampire-feeding-on-target", ("vampire", uid)), uid, target, PopupType.LargeCaution);
    }

    private void OnFeedEnd(EntityUid uid, AbilityFeedComponent? component, VampireEventFeedAbilityDoAfter args)
    {
        // if canceled return (maybe spray blood?)
        if (args.Handled || args.Cancelled)
            return;

        if (!Resolve(uid, ref component) || !TryComp<VampireComponent>(uid, out var vampire))
            return;

        var target = args.Args.Target;
        if (target is not { } || !TryComp<BloodstreamComponent>(target, out var targetBloodstream))
            return;

        if (!TryComp<BloodEssenceUserComponent>(uid, out var bloodEssenceUser))
            return;

        var collectedEssence = _bloodEssence.TryExtractBlood(uid, component.BloodPerFeed, target.Value, targetBloodstream);
        if (collectedEssence > 0.0f)
        {
            // todo: play sound
            _popup.PopupEntity(Loc.GetString("vampire-feeding-successful-vampire", ("target", target)), uid, uid, PopupType.Medium);
            _popup.PopupEntity(Loc.GetString("vampire-feeding-successful-target", ("vampire", uid)), uid, target.Value, PopupType.MediumCaution);
            _vampire.DepositEssence(uid, vampire, collectedEssence);
        }

        args.Handled = true;
    }
}
