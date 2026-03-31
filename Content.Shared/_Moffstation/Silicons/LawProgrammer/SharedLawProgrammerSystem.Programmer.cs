using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Silicons.Laws;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Silicons.LawProgrammer;

/// <summary>
/// This handles the law configurator
/// </summary>
public sealed partial class SharedLawProgrammerSystem
{
    [Dependency] private readonly SharedSiliconLawSystem _lawSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;


    /// <inheritdoc/>
    public void InitializeConfigurator ()
    {
        SubscribeLocalEvent<LawProgrammerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<LawProgrammerComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<LawProgrammerComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnInserted(Entity<LawProgrammerComponent> entity, ref EntInsertedIntoContainerMessage msg)
    {
        if (msg.Container.ID != entity.Comp.LawBoardSlot)
            return;

        Dirty(entity);
        _appearance.SetData(entity.Owner, LawReprogrammerVisuals.Status, LawReprogrammerStatus.Full);

        TryGetBoardLaws(entity, out var laws);
        _userInterface.SetUiState(entity.Owner, LawProgrammerUiKey.Key, GetBuiState(entity));
    }

    private void OnRemoved(Entity<LawProgrammerComponent> entity, ref EntRemovedFromContainerMessage msg)
    {
        if (msg.Container.ID != entity.Comp.LawBoardSlot)
            return;

        Dirty(entity);
        _appearance.SetData(entity.Owner, LawReprogrammerVisuals.Status, LawReprogrammerStatus.Empty);


        TryGetBoardLaws(entity, out var laws);
        _userInterface.SetUiState(entity.Owner, LawProgrammerUiKey.Key, GetBuiState(entity));
    }

    private void OnAfterInteract(Entity<LawProgrammerComponent> entity, ref AfterInteractEvent ev)
    {
        if (! ev.CanReach ||
            ev.Target is not { } target ||
            ! _entMan.TryGetComponent<LawProgrammerTargetComponent>(target, out var targetComp) ||
            ! TryGetBoardLaws(entity, out var laws))
            return;

        var beforeDoAfter = new BeforeProgramAttemptEvent(ev.User, ev.Used, entity.Comp.BaseAttemptDuration);
        RaiseLocalEvent(target, ref beforeDoAfter);

        if (! beforeDoAfter.CanProceed)
            return;

        var doAfterArgs = new DoAfterArgs(
            _entMan,
            beforeDoAfter.User,
            beforeDoAfter.Time,
            new ProgramAttemptDoAfterEvent(laws),
            target,
            target,
            ev.Used)
        {
            Hidden = false,
            AttemptFrequency = AttemptFrequency.EveryTick,
            BreakOnDamage = true,
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameTool
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
        ev.Handled = true;
    }

}

[ByRefEvent]
public sealed class BeforeProgramAttemptEvent(EntityUid user, EntityUid used, TimeSpan initialTime) : EntityEventArgs
{
    public bool CanProceed = false;
    public readonly EntityUid User = user;
    public readonly EntityUid Used = used;
    public TimeSpan Time = initialTime;
}

[Serializable, NetSerializable]
public sealed partial class ProgramAttemptDoAfterEvent: DoAfterEvent
{
    public readonly List<SiliconLaw> Laws;

    public ProgramAttemptDoAfterEvent(List<SiliconLaw> laws)
    {
        Laws = laws;
    }

    public override DoAfterEvent Clone() => this;
}


[Serializable, NetSerializable]
public enum LawReprogrammerVisuals : byte
{
    Status,
}

[Serializable, NetSerializable]
public enum LawReprogrammerStatus : byte
{
    Empty,
    Full,
}
