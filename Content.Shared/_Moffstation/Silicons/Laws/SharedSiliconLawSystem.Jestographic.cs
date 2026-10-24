using Content.Shared._Moffstation.JestographicSequencer;
using Content.Shared.Mind.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;

namespace Content.Shared.Silicons.Laws;

public abstract partial class SharedSiliconLawSystem
{
    //Mirrors SharedSiliconLawSystem.Emag, but swaps in the sequencer's lawset instead of appending emag laws.
    public void InitializeJestographic()
    {
        SubscribeLocalEvent<BorgChassisComponent, GotJestedEvent>(OnChassisJested);
        SubscribeLocalEvent<BorgBrainComponent, GotJestedEvent>(OnBrainJested);
    }

    private void OnChassisJested(Entity<BorgChassisComponent> ent, ref GotJestedEvent args)
    {
        EntityUid providerUid;
        SiliconLawProviderComponent provider;

        if (TryComp<SiliconLawProviderComponent>(ent, out var chassisProvider))
        {
            providerUid = ent.Owner;
            provider = chassisProvider;
        }
        else if (ent.Comp.BrainEntity is { } brain && TryComp<SiliconLawProviderComponent>(brain, out var brainProvider))
        {
            providerUid = brain;
            provider = brainProvider;
        }
        else
        {
            if (ent.Comp.BrainEntity == null)
                _popup.PopupEntity(Loc.GetString("law-emag-cannot-brainless", ("entity", ent)), ent, args.UserUid);

            return;
        }

        if (provider.Subverted)
        {
            _popup.PopupEntity(Loc.GetString("law-emag-already-emagged", ("entity", providerUid)), ent, args.UserUid);
            return;
        }

        if (!CanBeEmagged(providerUid, args.UserUid, out var reason, out var emagLawcomp, ent.Owner))
        {
            _popup.PopupEntity(reason, ent, args.UserUid);
            return;
        }

        var foundMind = TryComp<MindContainerComponent>(ent, out var mindContainer) && mindContainer.HasMind;
        if (!foundMind
            && ent.Comp.BrainEntity is { } brainId
            && TryComp<MindContainerComponent>(brainId, out var brainMindContainer)
            && brainMindContainer.HasMind)
        {
            foundMind = true;
        }

        if (!foundMind)
        {
            _popup.PopupEntity(Loc.GetString("law-emag-require-mind", ("entity", providerUid)), ent, args.UserUid);
            return;
        }

        provider.Subverted = true;
        //SetProviderLaws((providerUid, provider), GetLawset(args.Lawset).Laws, cue: emagLawcomp.EmaggedSound); //Add this back and remove lower line when alternate emag line recorded
        SetProviderLaws((providerUid, provider), GetLawset(args.Lawset).Laws, silent: true);
        Dirty(providerUid, provider);

        emagLawcomp.OwnerName = Name(args.UserUid);

        _stunSystem.TryUpdateParalyzeDuration(ent, emagLawcomp.StunTime);

        args.Handled = true;
    }

    private void OnBrainJested(Entity<BorgBrainComponent> ent, ref GotJestedEvent args)
    {
        if (!TryComp<SiliconLawBoundComponent>(ent, out _)
            || !TryComp<SiliconLawProviderComponent>(ent, out var brainProvider))
            return;

        if (brainProvider.Subverted)
        {
            _popup.PopupEntity(Loc.GetString("law-emag-already-emagged", ("entity", ent)), ent, args.UserUid);
            return;
        }

        if (!CanBeEmagged(ent, args.UserUid, out var reason, out var emagLawcomp))
        {
            _popup.PopupEntity(reason, ent, args.UserUid);
            return;
        }

        if (!TryComp<MindContainerComponent>(ent, out var mindContainer) || !mindContainer.HasMind)
        {
            _popup.PopupEntity(Loc.GetString("law-emag-require-mind", ("entity", ent)), ent, args.UserUid);
            return;
        }

        brainProvider.Subverted = true;
        SetProviderLaws((ent, brainProvider), GetLawset(args.Lawset).Laws, cue: emagLawcomp.EmaggedSound);
        Dirty(ent, brainProvider);

        emagLawcomp.OwnerName = Name(args.UserUid);

        args.Handled = true;
    }
}
