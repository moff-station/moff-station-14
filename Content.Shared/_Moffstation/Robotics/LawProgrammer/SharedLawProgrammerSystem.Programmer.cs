using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Robotics.LawProgrammer;

/// <summary>
/// This handles the law programmer item
/// </summary>
public sealed partial class SharedLawProgrammerSystem
{
    [Dependency] private readonly SharedSiliconLawSystem _lawSystem = default!;
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

        var beforeDoAfter = new BeforeProgramAttemptEvent(ev.User, ev.Used, entity.Comp.BaseAttemptDuration, entity.Comp.RequireMind);
        RaiseLocalEvent(target, ref beforeDoAfter);

        if (! beforeDoAfter.CanProceed)
            return;

        ExpressAttempt(ev.User, entity.Owner);

        var doAfterArgs = new DoAfterArgs(
            _entMan,
            beforeDoAfter.User,
            beforeDoAfter.Time,
            new ProgramAttemptDoAfterEvent(laws, entity.Comp.RequireMind),
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


    #region utility functions
    private void ExpressAttempt(EntityUid user, EntityUid? used)
    {
        if (!_entMan.TryGetComponent<LawProgrammerComponent>(used, out var comp))
            return;
        _audio.PlayPredicted(comp.AttemptSound, user, user);
    }

    private void ExpressSuccess(EntityUid user, EntityUid? used, EntityUid target)
    {
        if (!_entMan.TryGetComponent<LawProgrammerComponent>(used, out var comp))
            return;
        _popup.PopupClient(Loc.GetString("law-programmer-interaction-success"), target, user);
        _audio.PlayPredicted(comp.SuccessSound, user, user);
    }

    private void ExpressFailure(EntityUid user, EntityUid? used,  EntityUid target, string reason)
    {
        if (!_entMan.TryGetComponent<LawProgrammerComponent>(used, out var comp))
            return;
        _popup.PopupClient(reason, target, user);
        _audio.PlayPredicted(comp.FailureSound, user, user);
    }

    private LawProgrammerBuiState GetBuiState(Entity<LawProgrammerComponent> ent)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.LawBoardSlot, out var container) ||
            container.ContainedEntities.Count == 0)
        {
            return new LawProgrammerBuiState(null, null);
        }

        var board = container.ContainedEntities[0];

        string? name = null;
        // create the name
        if (_entMan.TryGetComponent<MetaDataComponent>(board, out var data))
        {
            name = data.EntityName.ToUpper();
            var attempt = name.Split(['(', ')']);
            if (attempt.Length > 1)
                name = attempt[1];
        }

        List<SiliconLaw>? laws = null;
        if (_entMan.TryGetComponent<SiliconLawProviderComponent>(board, out var provider))
        {
            laws = _lawSystem.GetLawset(provider.Laws).Laws;
        }

        return new LawProgrammerBuiState(name, laws);
    }
    #endregion
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
