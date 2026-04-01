using System.Diagnostics.CodeAnalysis;
using Content.Shared.DoAfter;
using Content.Shared.Mind.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Wires;

namespace Content.Shared._Moffstation.Robotics.LawProgrammer;

/// <summary>
/// this handle entities that can be targeted by a law programmer
/// </summary>
public sealed partial class SharedLawProgrammerSystem
{
    private void InitializeTarget()
    {
        SubscribeLocalEvent<LawProgrammerTargetComponent, BeforeProgramAttemptEvent>(OnBeforeProgramAttempt);
        SubscribeLocalEvent<LawProgrammerTargetComponent, DoAfterAttemptEvent<ProgramAttemptDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<LawProgrammerTargetComponent, ProgramAttemptDoAfterEvent>(OnDoAfterFinished);
    }

    private void OnBeforeProgramAttempt(Entity<LawProgrammerTargetComponent> ent, ref BeforeProgramAttemptEvent ev)
    {
        if (_entMan.TryGetComponent<BorgBrainComponent>(ent, out var brainComp))
        {
            if (!BrainHaveActiveMind((ent, brainComp)) && ev.NeedMind)
            {
                ExpressFailure(ev.User, ev.Used, ent, Loc.GetString("law-programmer-interaction-failure-target-absent"));
                ev.CanProceed = false;
            }
            else if (ent.Comp.IsImmune)
            {
                ExpressFailure(ev.User, ev.Used, ent, Loc.GetString("law-programmer-interaction-failure-target-immune"));
                ev.CanProceed = false;
            }
            else if (!TryGetBrainProvider((ent, brainComp), out _))
            {
                ExpressFailure(ev.User, ev.Used, ent, Loc.GetString("law-programmer-interaction-failure-provider-missing"));
                ev.CanProceed = false;
            }
            else
            {
                ev.Time *= ent.Comp.DurationMultiplier;
                ev.CanProceed = true;
            }
        }
        else if (_entMan.TryGetComponent<BorgChassisComponent>(ent, out var chassisComp))
        {
            if (!ChassisHaveOpenPanel((ent, chassisComp)))
            {
                ev.CanProceed = false;
            }
            else if (!ChassisHaveActiveMind((ent, chassisComp)) && ev.NeedMind)
            {
                ExpressFailure(ev.User, ev.Used, ent, Loc.GetString("law-programmer-interaction-failure-target-absent"));
                ev.CanProceed = false;
            }
            else if (ent.Comp.IsImmune)
            {
                ExpressFailure(ev.User, ev.Used, ent, Loc.GetString("law-programmer-interaction-failure-target-immune"));
                ev.CanProceed = false;
            }
            else if (!TryGetChassisProvider((ent, chassisComp), out _))
            {
                ExpressFailure(ev.User, ev.Used, ent, Loc.GetString("law-programmer-interaction-failure-provider-missing"));
                ev.CanProceed = false;
            }
            else
            {
                ev.Time *= ent.Comp.DurationMultiplier;
                ev.CanProceed = true;
            }
        }
    }

    private void OnDoAfterAttempt(Entity<LawProgrammerTargetComponent> ent,
        ref DoAfterAttemptEvent<ProgramAttemptDoAfterEvent> ev)
    {
        if (ev.Cancelled)
            return;

        if (_entMan.TryGetComponent<BorgBrainComponent>(ent, out var brainComp))
        {
            if (!BrainHaveActiveMind((ent, brainComp)) && ev.Event.NeedMind)
            {
                ExpressFailure(ev.Event.User, ev.Event.Used, ent, Loc.GetString("law-programmer-interaction-failure-target-absent"));
                ev.Event.CancelHandled = true;
                ev.Cancel();
            }
            else if (ent.Comp.IsImmune)
            {
                ExpressFailure(ev.Event.User, ev.Event.Used, ent, Loc.GetString("law-programmer-interaction-failure-target-immune"));
                ev.Event.CancelHandled = true;
                ev.Cancel();
            }
            else if (!TryGetBrainProvider((ent, brainComp), out _))
            {
                ExpressFailure(ev.Event.User, ev.Event.Used, ent, Loc.GetString("law-programmer-interaction-failure-provider-missing"));
                ev.Event.CancelHandled = true;
                ev.Cancel();
            }
        }
        else if (_entMan.TryGetComponent<BorgChassisComponent>(ent, out var chassisComp))
        {
            if (!ChassisHaveOpenPanel((ent, chassisComp)))
            {
                ExpressFailure(ev.Event.User, ev.Event.Used, ent, Loc.GetString("law-programmer-interaction-failure-target-unreachable-popup"));
                ev.Event.CancelHandled = true;
                ev.Cancel();
            }
            else if (!ChassisHaveActiveMind((ent, chassisComp)) && ev.Event.NeedMind)
            {
                ExpressFailure(ev.Event.User, ev.Event.Used, ent, Loc.GetString("law-programmer-interaction-failure-target-absent"));
                ev.Event.CancelHandled = true;
                ev.Cancel();
            }
            else if (ent.Comp.IsImmune)
            {
                ExpressFailure(ev.Event.User, ev.Event.Used, ent, Loc.GetString("law-programmer-interaction-failure-target-immune"));
                ev.Event.CancelHandled = true;
                ev.Cancel();
            }
            else if (!TryGetChassisProvider((ent, chassisComp), out _))
            {
                ExpressFailure(ev.Event.User, ev.Event.Used, ent, Loc.GetString("law-programmer-interaction-failure-target-absent-provider"));
                ev.Event.CancelHandled = true;
                ev.Cancel();
            }
        }
    }

    private void OnDoAfterFinished(Entity<LawProgrammerTargetComponent> ent,  ref ProgramAttemptDoAfterEvent ev)
    {
        if (ev.Cancelled)
        {
            if (! ev.CancelHandled)
                ExpressFailure(ev.User, ev.Used, ev.User, Loc.GetString("law-programmer-interaction-failure-target-unreachable-popup"));
            return;
        }


        if (_entMan.TryGetComponent<BorgBrainComponent>(ent, out var brainComp))
        {
            if (TryGetBrainProvider((ent, brainComp), out var brainProvider))
            {
                ExpressSuccess(ev.User, ev.Used, ent);
                _lawSystem.SetProviderLaws(brainProvider.Value, ev.Laws);
            }
            else
            {
                ExpressFailure(ev.User, ev.Used, ent, Loc.GetString("law-programmer-interaction-failure-provider-missing"));
            }
        }
        else if (_entMan.TryGetComponent<BorgChassisComponent>(ent, out var chassisComp))
        {
            if (TryGetChassisProvider((ent, chassisComp), out var chassisProvider))
            {
                ExpressSuccess(ev.User, ev.Used, ent);
                _lawSystem.SetProviderLaws(chassisProvider.Value, ev.Laws);
            }
            else
            {
                ExpressFailure(ev.User, ev.Used, ent, Loc.GetString("law-programmer-interaction-failure-provider-missing"));
            }
        }
    }


    #region utility functions
    private bool BrainHaveActiveMind(Entity<BorgBrainComponent> ent)
    {
        return _entMan.TryGetComponent<MindContainerComponent>(ent, out var comp) && comp.HasMind;
    }

    private bool TryGetBrainProvider(Entity<BorgBrainComponent> ent, [NotNullWhen(true)] out Entity<SiliconLawProviderComponent?>? provider)
    {
        provider = null;
        if (_entMan.TryGetComponent<SiliconLawProviderComponent>(ent, out var providerComp))
        {
            provider = (ent.Owner, providerComp);
            return true;
        }

        return false;
    }

    private bool ChassisHaveOpenPanel(Entity<BorgChassisComponent> ent)
    {
        return _entMan.TryGetComponent<WiresPanelComponent>(ent, out var panel) && panel.Open;
    }

    private bool ChassisHaveActiveMind(Entity<BorgChassisComponent> ent)
    {
        // does the chassis have a mind
        if (_entMan.TryGetComponent<MindContainerComponent>(ent, out var comp) && comp.HasMind)
            return true;
        // does the chassis have a brain with a mind
        if (ent.Comp.BrainContainer.ContainedEntity is { } brain &&
            _entMan.TryGetComponent<BorgBrainComponent>(brain, out var brainComp))
            return BrainHaveActiveMind((brain, brainComp));

        return false;
    }

    private bool TryGetChassisProvider(Entity<BorgChassisComponent> ent, [NotNullWhen(true)] out Entity<SiliconLawProviderComponent?>? provider)
    {
        provider = null;

        if (_entMan.TryGetComponent<SiliconLawProviderComponent>(ent, out var chassisProvider))
        {
            provider = (ent, chassisProvider);
            return true;
        }

        if (ent.Comp.BrainContainer.ContainedEntity is { } brain &&
            _entMan.TryGetComponent<BorgBrainComponent>(brain, out var brainComp))
        {
            return TryGetBrainProvider((brain, brainComp) , out provider);
        }

        return false;
    }
    #endregion

}
