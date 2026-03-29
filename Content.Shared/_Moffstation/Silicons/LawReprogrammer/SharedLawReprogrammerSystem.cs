using System.Diagnostics.CodeAnalysis;
using Content.Shared.Interaction;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Silicons.LawReprogrammer;

/// <summary>
/// This handles...
/// </summary>
public sealed class SharedLawReprogrammerSystem : EntitySystem
{
    [Dependency] private readonly SharedSiliconLawSystem _lawSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LawReprogrammerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<LawReprogrammerComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<LawReprogrammerComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnInserted(Entity<LawReprogrammerComponent> entity, ref EntInsertedIntoContainerMessage msg)
    {
        if (msg.Container.ID != entity.Comp.LawBoardSlot)
            return;

        // TryGetBoard
        if (TryGetLawBoard(entity, out var board))
            entity.Comp.LawSource = board; // our stuff
        else
            entity.Comp.LawSource = null;

        Dirty(entity);
        UpdateAppearance(entity);
    }

    private void OnRemoved(Entity<LawReprogrammerComponent> entity, ref EntRemovedFromContainerMessage msg)
    {
        if (msg.Container.ID != entity.Comp.LawBoardSlot)
            return;

        entity.Comp.LawSource = null;

        Dirty(entity);
        UpdateAppearance(entity);
    }

    private bool TryGetLawBoard(Entity<LawReprogrammerComponent> entity, [NotNullWhen(true)] out EntityUid? board)
    {
        board = null;

        if (!_containerSystem.TryGetContainer(entity, entity.Comp.LawBoardSlot, out var container) ||
            container.ContainedEntities.Count == 0)
            return false;

        board = container.ContainedEntities[0];
        return true;
    }

    private void UpdateAppearance(Entity<LawReprogrammerComponent> ent)
    {

    }


    private void OnAfterInteract(Entity<LawReprogrammerComponent> entity, ref AfterInteractEvent ev)
    {
        if (!ev.CanReach || ev.Target is not { } target)
            return;

        ev.Handled = TryReprogram(entity, target);
    }

    private bool TryReprogram(Entity<LawReprogrammerComponent> source, EntityUid target)
    {
        if (!TryComp<SiliconLawBoundComponent>(target, out var lawBoundComp) || !TryGetLawBoard(source, out var board))
            return false;

        if (source.Comp.NextAllowedUsed > _timing.CurTime)
            return false;

        if (_tagSystem.HasTag(target, source.Comp.ImmuneTag))
            return false;

        _lawSystem.

        return true;
    }
}
