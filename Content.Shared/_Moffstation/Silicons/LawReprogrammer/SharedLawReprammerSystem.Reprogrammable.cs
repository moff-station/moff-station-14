using Content.Shared.Mind.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Wires;
using Robust.Shared.Audio;

namespace Content.Shared._Moffstation.Silicons.LawReprogrammer;

/// <summary>
/// this handle reprogrammable devices
/// </summary>
public sealed partial class SharedLawReprogrammerSystem
{
    private void InitReprogrammable()
    {
        SubscribeLocalEvent<LawReprogrammableComponent, GotReprogrammedEvent>(OnReprogrammed);
    }

    private void OnReprogrammed(Entity<LawReprogrammableComponent> ent, ref GotReprogrammedEvent args)
    {
        args.Handled = true;

        if (ent.Comp.IsImmune)
        {
            args.Succeeded = false;
            _popup.PopupClient(Loc.GetString("reprogrammer-failure-no-aut-popup"), ent.Owner, args.UserUid);
        }
        else if (_entMan.TryGetComponent<BorgChassisComponent>(ent.Owner, out var chassis))
        {
            // only interact on chassis if their maintainance panel is open.
            if (_entMan.TryGetComponent<WiresPanelComponent>(ent.Owner, out var panel) && !panel.Open)
            {
                args.Handled = false;
                return;
            }
            args.Succeeded = TryReprogramChassis((ent.Owner, chassis), ref args);
        }
        else if (_entMan.TryGetComponent<BorgBrainComponent>(ent.Owner, out var brain))
        {
            args.Succeeded = TryReprogramBrain((ent.Owner, brain), ref args);
        }

        if (args.Succeeded)
            _popup.PopupClient(Loc.GetString("reprogrammer-success-popup"), ent.Owner, args.UserUid);
    }

    private bool TryReprogramChassis(Entity<BorgChassisComponent> ent, ref readonly GotReprogrammedEvent args)
    {
        // Check for law provider
        Entity<SiliconLawProviderComponent?>? provider = null;

        if (_entMan.TryGetComponent<SiliconLawProviderComponent>(ent, out var chassisProvider))
            provider = (ent, chassisProvider);
        else if (ent.Comp.BrainEntity is { } brain && _entMan.TryGetComponent<SiliconLawProviderComponent>(brain, out var brainProvider))
            provider = (brain, brainProvider);

        // Check for mind
        bool mindFound = false;

        if (TryComp<MindContainerComponent>(ent, out var chassisContainer) && chassisContainer.HasMind)
            mindFound = true;
        else if (ent.Comp.BrainEntity is { } brain &&
                 _entMan.TryGetComponent<MindContainerComponent>(brain, out var brainContainer) &&
                 brainContainer.HasMind)
            mindFound = true;

        // can only reprogram active units with a law provider.
        if (provider is { } prov && mindFound)
        {
            _lawSystem.SetProviderLaws(prov, args.Lawset.Laws);
            return true;
        }

        _popup.PopupClient(Loc.GetString("reprogrammer-failure-no-answer-popup"), ent.Owner, args.UserUid);
        return false;
    }

    private bool TryReprogramBrain(Entity<BorgBrainComponent> ent, ref readonly GotReprogrammedEvent args)
    {
        if (!TryComp<SiliconLawBoundComponent>(ent, out var lawBound) ||
            !TryComp<SiliconLawProviderComponent>(ent, out var provider) ||
            !TryComp<MindContainerComponent>(ent, out var mindContainer) ||
            !mindContainer.HasMind)
        {
            _popup.PopupClient(Loc.GetString("reprogrammer-failure-no-answer-popup"), ent.Owner, args.UserUid);
            return false;
        }

        _lawSystem.SetProviderLaws((ent, provider), args.Lawset.Laws);
        return true;
    }
}
