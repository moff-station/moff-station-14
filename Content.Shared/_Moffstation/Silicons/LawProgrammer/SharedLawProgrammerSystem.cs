using System.Diagnostics.CodeAnalysis;
using Content.Shared.Popups;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._Moffstation.Silicons.LawProgrammer;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class SharedLawProgrammerSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        InitializeConfigurator();
        InitializeTarget();
    }

    // this is an evil way to do this but i just don't know how to do it the correct way
    // (i.e go fetch the lawset on the server side then get it back in shared)
    private bool TryGetBoardLaws(Entity<LawProgrammerComponent> ent, [NotNullWhen(true)] out List<SiliconLaw>? laws)
    {
        laws = null;
        if (!_container.TryGetContainer(ent, ent.Comp.LawBoardSlot, out var container) ||
            container.ContainedEntities.Count == 0)
            return false;

        var board = container.ContainedEntities[0];

        if (!_entMan.TryGetComponent<SiliconLawProviderComponent>(board, out var provider))
            return false;

        laws = _lawSystem.GetLawset(provider.Laws).Laws;
        return true;
    }

    private void ExpressSuccess(EntityUid user, EntityUid? used, EntityUid target)
    {
        if (!_entMan.TryGetComponent<LawProgrammerComponent>(used, out var comp))
            return;
        _popup.PopupClient(Loc.GetString("programmer-interaction-success-popup"), target, user);
        _audio.PlayPredicted(comp.SuccessSound, used.Value, null);
    }

    private void ExpressFailure(EntityUid user, EntityUid? used,  EntityUid target, string reason)
    {
        if (!_entMan.TryGetComponent<LawProgrammerComponent>(used, out var comp))
            return;
        _popup.PopupClient(reason, target, user);
        _audio.PlayPredicted(comp.FailureSound, used.Value, null);
    }



    // put that on configurator
    private void UpdateBuiState()
    {

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
}

