using System.Diagnostics.CodeAnalysis;
using Content.Shared.DoAfter;
using Content.Shared.Mind.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Wires;

namespace Content.Shared._Moffstation.Silicons.LawProgrammer;

/// <summary>
/// this handle entities that can be targeted by a law configurator
/// </summary>
public sealed partial class SharedLawProgrammerSystem
{
    private void InitializeTarget()
    {
        SubscribeLocalEvent<LawProgrammerTargetComponent, BeforeProgramAttemptEvent>(OnBeforeBeingConfigured);
        SubscribeLocalEvent<LawProgrammerTargetComponent, DoAfterAttemptEvent<ProgramAttemptDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<LawProgrammerTargetComponent, ProgramAttemptDoAfterEvent>(OnDoAfterFinished);
    }

    private void OnBeforeBeingConfigured(Entity<LawProgrammerTargetComponent> ent, ref BeforeProgramAttemptEvent ev)
    {
        if (_entMan.TryGetComponent<BorgBrainComponent>(ent, out var brain))
        {
            if (ent.Comp.IsImmune)
            {
                ExpressFailure(
                    ev.User,
                    ev.Used,
                    ent,
                    Loc.GetString("programmer-interaction-failure-target-immune-popup"));
                ev.CanProceed = false;
            }
            else if (!_entMan.TryGetComponent<MindContainerComponent>(ent, out var container) || !container.HasMind)
            {
                ExpressFailure(
                    ev.User,
                    ev.Used,
                    ent,
                    Loc.GetString("programmer-interaction-failure-target-absent-popup"));
                ev.CanProceed = false;
            }
            else
            {
                ev.CanProceed = true;
            }
        }
        else if (_entMan.TryGetComponent<BorgChassisComponent>(ent, out var chassis))
        {
            if (_entMan.TryGetComponent<WiresPanelComponent>(ent, out var panel) && !panel.Open)
            {
                ev.CanProceed = false;
            }
            else if (ent.Comp.IsImmune)
            {
                ExpressFailure(
                    ev.User,
                    ev.Used,
                    ent,
                    Loc.GetString("programmer-interaction-failure-target-immune-popup"));
                ev.CanProceed = false;
            }
            else if (! TryGetChassisMindContainer((ent, chassis), out var container) || ! container.HasMind)
            {
                ExpressFailure(
                    ev.User,
                    ev.Used,
                    ent,
                    Loc.GetString("programmer-interaction-failure-target-mind-popup"));
                ev.CanProceed = false;
            }
            else
            {
                ev.CanProceed = true;
            }
        }
        else
        {
            ev.CanProceed = false;
        }
    }

    private void OnDoAfterAttempt(Entity<LawProgrammerTargetComponent> ent,
        ref DoAfterAttemptEvent<ProgramAttemptDoAfterEvent> ev)
    {
        if (_entMan.TryGetComponent<BorgBrainComponent>(ent, out var brain))
        {
            if (!_entMan.TryGetComponent<SiliconLawProviderComponent>(ent, out _) ||
                !_entMan.TryGetComponent<MindContainerComponent>(ent, out var mindContainer) ||
                !mindContainer.HasMind)
            {
                ExpressFailure(
                    ev.Event.User,
                    ev.Event.Used,
                    ent,
                    Loc.GetString("programmer-interaction-failure-target-unreachable-popup"));
                ev.Cancel();
            }
        }
        else if (_entMan.TryGetComponent<BorgChassisComponent>(ent, out var chassis))
        {
            if (_entMan.TryGetComponent<WiresPanelComponent>(ent, out var panel) && !panel.Open)
            {
                ExpressFailure(
                    ev.Event.User,
                    ev.Event.Used,
                    ent,
                    Loc.GetString("programmer-interaction-failure-target-unreachable-popup"));
                ev.Cancel();
            }
            else if (!TryGetChassisProvider((ent, chassis), out _) ||
                     !TryGetChassisMindContainer((ent, chassis), out var mindContainer) ||
                     !mindContainer.HasMind)
            {
                ExpressFailure(
                    ev.Event.User,
                    ev.Event.Used,
                    ent,
                    Loc.GetString("programmer-interaction-failure-target-unreachable-popup"));
                ev.Cancel();
            }
        }
    }

    private void OnDoAfterFinished(Entity<LawProgrammerTargetComponent> ent,  ref ProgramAttemptDoAfterEvent ev)
    {
        if (_entMan.TryGetComponent<BorgBrainComponent>(ent, out var brain))
        {
            if (_entMan.TryGetComponent<SiliconLawProviderComponent>(ent, out var provider) &&
                _entMan.TryGetComponent<MindContainerComponent>(ent, out var container) &&
                container.HasMind)
            {
                ExpressSuccess(
                    ev.User,
                    ev.Used,
                    ent);
                _lawSystem.SetProviderLaws((ent, provider), ev.Laws);
            }
        }
        else if (_entMan.TryGetComponent<BorgChassisComponent>(ent, out var chassis))
        {
            if (TryGetChassisProvider((ent, chassis), out var providerEntity) &&
                TryGetChassisMindContainer((ent, chassis), out var container) &&
                container.HasMind)
            {
                ExpressSuccess(
                    ev.User,
                    ev.Used,
                    ent);
                _lawSystem.SetProviderLaws(providerEntity.Value, ev.Laws);
            }
        }
    }

    private bool TryGetChassisProvider(Entity<BorgChassisComponent> ent, [NotNullWhen(true)] out Entity<SiliconLawProviderComponent?>? provider)
    {
        provider = null;

        if (_entMan.TryGetComponent<SiliconLawProviderComponent>(ent, out var chassisProvider))
        {
            provider = (ent, chassisProvider);
            return true;
        }
        if (ent.Comp.BrainEntity is { } brain &&
            _entMan.TryGetComponent<SiliconLawProviderComponent>(brain, out var brainProvider))
        {
            provider = (brain, brainProvider);
            return true;
        }

        return false;
    }

    private bool TryGetChassisMindContainer(Entity<BorgChassisComponent> ent, [NotNullWhen(true)] out MindContainerComponent? mindContainer)
    {
        mindContainer = null;

        if (_entMan.TryGetComponent<MindContainerComponent>(ent, out var chassisMind))
        {
            mindContainer = chassisMind;
            return true;
        }
        else if (ent.Comp.BrainEntity is { } brain &&
                 _entMan.TryGetComponent<MindContainerComponent>(brain, out var brainMind))
        {
            mindContainer = brainMind;
            return true;
        }

        return false;
    }

    // TODO : use these function to clean up that stuff a bit.
    private bool TryFindActiveMind()
    {
        return false;
    }

    private bool TryGetProvider()
    {
        return false;
    }
}
