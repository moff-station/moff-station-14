using Content.Shared._Moffstation.Silicons.LawReprogrammer;
using Content.Shared.Mind.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;

namespace Content.Shared.Silicons.Laws;

// Moffstation - Cyborg law alteration

public abstract partial class SharedSiliconLawSystem
{
    public void InitializeReprogrammer()
    {
        SubscribeLocalEvent<BorgChassisComponent, GotReprogrammedEvent>(OnChassisReprogrammed);
        SubscribeLocalEvent<BorgBrainComponent, GotReprogrammedEvent>(OnBrainReprogrammed);
    }

    private void OnChassisReprogrammed(Entity<BorgChassisComponent> ent, ref GotReprogrammedEvent args)
    {
        // Determine the provider
        EntityUid providerUid;
        SiliconLawProviderComponent provider;

        // 1. Check if chassis is provider
        if (TryComp<SiliconLawProviderComponent>(ent, out var chassisProvider))
        {
            providerUid = ent.Owner;
            provider = chassisProvider;
        }
        // 2. Check if brain is provider
        else if (ent.Comp.BrainEntity is { } brain && TryComp<SiliconLawProviderComponent>(brain, out var brainProvider))
        {
            providerUid = brain;
            provider = brainProvider;
        }
        else
        {
            // If no brain and chassis is not provider
            if (ent.Comp.BrainEntity == null)
                _popup.PopupClient(Loc.GetString("reprogrammer-no-occupant-popup", ("entity", ent)), ent, args.UserUid);

            return;
        }

        bool foundMind = false;
        if (TryComp<MindContainerComponent>(ent, out var mindContainer) && mindContainer.HasMind) // Check chassis for a mind first.
        {
            foundMind = true;
        }
        else if (ent.Comp.BrainEntity is { } brainId && TryComp<MindContainerComponent>(brainId, out var brainMindContainer) && brainMindContainer.HasMind) // Then check the brain.
        {
            foundMind = true;
        }

        if (!foundMind)
        {
            _popup.PopupClient(Loc.GetString("law-emag-require-mind", ("entity", providerUid)), ent, args.UserUid);
            return;
        }

        SetProviderLaws((providerUid, provider), args.Lawset.Laws);
        Dirty(providerUid, provider);

        // todo : do we paralyse ?

        args.Handled = true;
    }

    private void OnBrainReprogrammed(Entity<BorgBrainComponent> ent, ref GotReprogrammedEvent args)
    {
        if (!TryComp<SiliconLawBoundComponent>(ent, out var lawboundComp)
            || !TryComp<SiliconLawProviderComponent>(ent, out var brainProvider))
            return;

        // The brain must have a mind to be emagged.
        if (!TryComp<MindContainerComponent>(ent, out var mindContainer) || !mindContainer.HasMind)
        {
            _popup.PopupClient(Loc.GetString("law-emag-require-mind", ("entity", ent)), ent, args.UserUid);
            return;
        }

        brainProvider.Subverted = true;
        SetProviderLaws((ent, brainProvider), args.Lawset.Laws);
        Dirty(ent, brainProvider);

        args.Handled = true;
    }
}

// Moffstation - End
