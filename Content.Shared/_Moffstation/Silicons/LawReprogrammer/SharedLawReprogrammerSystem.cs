using System.Diagnostics.CodeAnalysis;
using Content.Shared.Interaction;
using Content.Shared.Popups;
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
    [Dependency] private readonly IEntityManager _entMan = default!;

    [Dependency] private readonly SharedPopupSystem _popup = default!;


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

        Dirty(entity);
        UpdateAppearance(entity);
    }

    private void OnRemoved(Entity<LawReprogrammerComponent> entity, ref EntRemovedFromContainerMessage msg)
    {
        if (msg.Container.ID != entity.Comp.LawBoardSlot)
            return;

        Dirty(entity);
        UpdateAppearance(entity);
    }

    private bool TryGetLaws(Entity<LawReprogrammerComponent> entity, EntityUid user, [NotNullWhen(true)] out SiliconLawset? lawset)
    {
        lawset = null;

        if (!_containerSystem.TryGetContainer(entity, entity.Comp.LawBoardSlot, out var container) ||
            container.ContainedEntities.Count == 0)
        {
            _popup.PopupClient("we got no law provider !", user, user);
            return false;
        }

        var board =  container.ContainedEntities[0];

        // ideally we would use the function of the API but they don't see to work don't ask me why.
        if (!_entMan.TryGetComponent<SiliconLawProviderComponent>(board, out var provider))
        {
            _popup.PopupClient("board have no law provider", user, user);
            return false;
        }

        lawset = provider.Lawset;

        _popup.PopupClient("provider found ! laws : " + lawset.Laws.Count, user, user);
        return true;
    }

    private void UpdateAppearance(Entity<LawReprogrammerComponent> ent)
    {
        // TODO ! (empty, full and colors)
    }


    private void OnAfterInteract(Entity<LawReprogrammerComponent> entity, ref AfterInteractEvent ev)
    {
        if (!ev.CanReach || ev.Target is not { } target)
            return;

        ev.Handled = TryReprogram(entity, target, ev.User);
    }

    private bool TryReprogram(Entity<LawReprogrammerComponent> source, EntityUid target, EntityUid user)
    {
        if (_timing.CurTime < source.Comp.NextAllowedUsed || _tagSystem.HasTag(target, source.Comp.ImmuneTag))
            return false;

        if (!TryGetLaws(source, user, out var lawset))
            return false;

        var ev = new GotReprogrammedEvent(source.Owner, lawset);
        RaiseLocalEvent(target, ref ev);

        if (!ev.Handled)
            return false;

        source.Comp.NextAllowedUsed = _timing.CurTime + source.Comp.DelayBetweenUses;
        return true;
    }
}

[ByRefEvent]
public record struct GotReprogrammedEvent(EntityUid UserUid, SiliconLawset Lawset, bool Handled = false);
