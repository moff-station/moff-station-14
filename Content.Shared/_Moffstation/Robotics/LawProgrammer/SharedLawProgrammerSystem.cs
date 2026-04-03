using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Wires;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Robotics.LawProgrammer;

/// <summary>
/// This handles the law programmer
/// </summary>
public sealed partial class SharedLawProgrammerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedSiliconLawSystem _lawSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedWiresSystem _wires = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LawProgrammerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<LawProgrammerComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<LawProgrammerTargetComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<LawProgrammerTargetComponent, DoAfterAttemptEvent<ProgramLawsDoAfter>>(OnDoAfterAttempt);
        SubscribeLocalEvent<LawProgrammerTargetComponent, ProgramLawsDoAfter>(OnProgramLawsDoAfter);
    }

    private void OnInserted(Entity<LawProgrammerComponent> entity, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != entity.Comp.LawBoardSlot)
            return;

        _appearance.SetData(entity.Owner, LawReprogrammerVisuals.Filled, true);
        UpdateUi(entity);
    }

    private void OnRemoved(Entity<LawProgrammerComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != entity.Comp.LawBoardSlot)
            return;

        _appearance.SetData(entity.Owner, LawReprogrammerVisuals.Filled, true);
        UpdateUi(entity);
    }

    private void OnInteractUsing(Entity<LawProgrammerTargetComponent> target, ref InteractUsingEvent args)
    {
        if (args.Handled || !TryComp<LawProgrammerComponent>(args.Used, out var programmer))
            return;

        var entity = new Entity<LawProgrammerComponent>(args.Used, programmer);
        if (GetInsertedLawProvider(entity) is null)
            return;

        if (!CanReprogram(entity, target, out var failureReason))
        {
            if (failureReason is not null)
            {
                _popup.PopupClient(Loc.GetString(failureReason), target, args.User);
                _audio.PlayPredicted(entity.Comp.FailureSound, entity, args.User);
            }

            return;
        }

        _audio.PlayPredicted(entity.Comp.AttemptSound, entity, args.User);
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            args.User,
            entity.Comp.BaseAttemptDuration * target.Comp.DurationMultiplier,
            new ProgramLawsDoAfter(),
            target,
            target,
            args.Used)
        {
            Hidden = false,
            AttemptFrequency = AttemptFrequency.EveryTick,
            BreakOnDamage = true,
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameTool
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDoAfterAttempt(Entity<LawProgrammerTargetComponent> entity,
        ref DoAfterAttemptEvent<ProgramLawsDoAfter> args)
    {
        if (args.Cancelled)
            return;

        if (args.Event.Used is not { } used || !TryComp<LawProgrammerComponent>(args.Event.Used, out var programmer))
        {
            args.Cancel();
            return;
        }

        if (!CanReprogram((used, programmer), entity, out var failureReason))
        {
            args.Event.CancellationReason = failureReason;
            args.Cancel();
        }
    }

    private void OnProgramLawsDoAfter(Entity<LawProgrammerTargetComponent> target, ref ProgramLawsDoAfter args)
    {
        if (args.Used is not { } u || !TryComp<LawProgrammerComponent>(u, out var programmer))
            return;

        var used = new Entity<LawProgrammerComponent>(u, programmer);
        if (GetInsertedLawProvider(used) is not { } lawProvider)
            return;

        if (args.Cancelled)
        {
            if (args.CancellationReason is { } reason)
            {
                _popup.PopupClient(Loc.GetString(reason), args.User, args.User);
                _audio.PlayPredicted(used.Comp.FailureSound, used, args.User);
            }

            return;
        }

        if (_netMan.IsClient)
            return;

        if (GetLawProvidingBrainOfTarget(target.AsNullable()) is { } targetBrain)
        {
            _popup.PopupClient(Loc.GetString("law-programmer-interaction-success"), target, args.User);
            _audio.PlayPredicted(used.Comp.SuccessSound, used, args.User);
            _lawSystem.SetProviderLaws((targetBrain, targetBrain.Comp3), _lawSystem.GetProviderLaws(lawProvider.AsNullable()).Laws);
        }
        else
        {
            _popup.PopupClient(Loc.GetString("law-programmer-interaction-failure-provider-missing"), target, args.User);
            _audio.PlayPredicted(used.Comp.FailureSound, used, args.User);
        }
    }

    private void UpdateUi(Entity<LawProgrammerComponent> entity)
    {
        LawProgrammerBuiState state;
        if (GetInsertedLawProvider(entity) is not { } board)
        {
            state = new LawProgrammerBuiState(null, null);
        }
        else
        {
            var name = Name(board);
            var lawsetName = name.Split('(', ')') switch
            {
                [_, var e, ..] => e,
                _ => name,
            };
            state = new LawProgrammerBuiState(lawsetName.ToUpper(), _lawSystem.GetLawset(board.Comp.Laws).Laws);
        }

        _userInterface.SetUiState(entity.Owner, LawProgrammerUiKey.Key, state);
    }

    private Entity<SiliconLawProviderComponent>? GetInsertedLawProvider(Entity<LawProgrammerComponent> entity)
    {
        return _itemSlots.GetItemOrNull(entity, entity.Comp.LawBoardSlot) is { } board &&
               CompOrNull<SiliconLawProviderComponent>(board) is { } laws
            ? (board, laws)
            : null;
    }

    private bool CanReprogram(
        Entity<LawProgrammerComponent> programmer,
        Entity<LawProgrammerTargetComponent> target,
        out LocId? failureReason)
    {
        failureReason = null;
        if (GetInsertedLawProvider(programmer) is null)
            return false;

        if (target.Comp.IsImmune)
        {
            failureReason = "law-programmer-interaction-failure-target-immune";
            return false;
        }

        if (HasComp<BorgChassisComponent>(target))
        {
            // can't reprogram a closed borg.
            if (!_wires.IsPanelOpen(target.Owner))
                return false;

            // when inserted mind is on chassis entity.
            if (_mind.GetMind(target) is null)
            {
                failureReason = "law-programmer-interaction-failure-target-absent";
                return false;
            }

            if (GetLawProvidingBrainOfTarget(target.AsNullable()) is null)
            {
                failureReason = "law-programmer-interaction-failure-provider-missing";
                return false;
            }

            return true;
        }

        if (GetLawProvidingBrainOfTarget(target.AsNullable()) is not { } brain)
        {
            failureReason = "law-programmer-interaction-failure-provider-missing";
            return false;
        }

        if (programmer.Comp.RequireMind && _mind.GetMind(brain) is null)
        {
            failureReason = "law-programmer-interaction-failure-target-absent";
            return false;
        }

        return true;
    }

    private Entity<LawProgrammerTargetComponent, BorgBrainComponent, SiliconLawProviderComponent>?
        GetLawProvidingBrainOfTarget(Entity<LawProgrammerTargetComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return null;

        if (TryComp<BorgBrainComponent>(entity, out var brainComp))
        {
            if (TryComp<SiliconLawProviderComponent>(entity, out var lawProvider))
                return (entity, entity.Comp, brainComp, lawProvider);
        }

        if (TryComp<BorgChassisComponent>(entity, out var chassisComp) &&
            chassisComp.BrainEntity is { } brain)
        {
            return GetLawProvidingBrainOfTarget(brain);
        }

        return null;
    }
}

[Serializable, NetSerializable]
public sealed partial class ProgramLawsDoAfter : DoAfterEvent
{
    public LocId? CancellationReason;

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public enum LawReprogrammerVisuals : byte
{
    Filled,
}
