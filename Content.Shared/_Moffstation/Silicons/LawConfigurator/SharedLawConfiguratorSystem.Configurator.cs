using System.Diagnostics.CodeAnalysis;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Tag;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Silicons.LawConfigurator;

/// <summary>
/// This handles the reprogrammer
/// </summary>
public sealed partial class SharedLawConfiguratorSystem
{
    [Dependency] private readonly SharedSiliconLawSystem _lawSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    [Dependency] private readonly SharedPopupSystem _popup = default!;


    /// <inheritdoc/>
    public void InitializeConfigurator ()
    {
        SubscribeLocalEvent<LawConfiguratorComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<LawConfiguratorComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<LawConfiguratorComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnInserted(Entity<LawConfiguratorComponent> entity, ref EntInsertedIntoContainerMessage msg)
    {
        if (msg.Container.ID != entity.Comp.LawBoardSlot)
            return;

        Dirty(entity);
        _appearance.SetData(entity.Owner, LawReprogrammerVisuals.Status, LawReprogrammerStatus.Full);
    }

    private void OnRemoved(Entity<LawConfiguratorComponent> entity, ref EntRemovedFromContainerMessage msg)
    {
        if (msg.Container.ID != entity.Comp.LawBoardSlot)
            return;

        Dirty(entity);
        _appearance.SetData(entity.Owner, LawReprogrammerVisuals.Status, LawReprogrammerStatus.Empty);
    }

    private bool TryGetLaws(Entity<LawConfiguratorComponent> entity, EntityUid user, [NotNullWhen(true)] out SiliconLawset? lawset)
    {
        lawset = null;

        if (!_containerSystem.TryGetContainer(entity, entity.Comp.LawBoardSlot, out var container) ||
            container.ContainedEntities.Count == 0)
            return false;

        var board =  container.ContainedEntities[0];

        // ideally we would use the function of the API but they don't see to work don't ask me why.
        if (!_entMan.TryGetComponent<SiliconLawProviderComponent>(board, out var provider))
            return false;

        lawset = provider.Lawset;

        return true;
    }

    private void OnAfterInteract(Entity<LawConfiguratorComponent> entity, ref AfterInteractEvent ev)
    {
        if (!ev.CanReach || ev.Target is not { } target)
            return;

        ev.Handled = TryReprogram(entity, target, ev.User);
    }

    private bool TryReprogram(Entity<LawConfiguratorComponent> source, EntityUid target, EntityUid user)
    {
        if (_useDelay.IsDelayed(source.Owner) || !TryGetLaws(source, target, out var laws))
            return false;

        var ev = new GotReprogrammedEvent(user, laws);
        _entMan.EventBus.RaiseLocalEvent(target, ref ev);

        if (!ev.Handled)
            return false;

        //source.Comp.NextAllowedUse = _timing.CurTime + source.Comp.DelayBetweenUses;

        if (ev.Succeeded)
            _audio.PlayPredicted(source.Comp.SuccessSound, source.Owner, user, null);
        else
            _audio.PlayPredicted(source.Comp.FailureSound, source.Owner, user, null);

        _useDelay.TryResetDelay(source.Owner);
        return true;
    }
}

/// <summary>
/// Event sent when we attempt to reprogram an entity.
/// </summary>
/// <param name="UserUid"> entity </param>
/// <param name="Lawset"> lawset to be uploaded on success </param>
/// <param name="Handled"> true if the entity reacted to the attempt </param>
/// <param name="Succeeded"> true if the lawset was succesfully uploaded </param>
[ByRefEvent]
public record struct GotReprogrammedEvent(
    EntityUid UserUid,
    SiliconLawset Lawset,
    bool Handled = false,
    bool Succeeded = false);


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
} // todo : do different colors ?
