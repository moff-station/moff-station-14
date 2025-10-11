using Content.Server.CartridgeLoader.Cartridges;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.CartridgeLoader.Cartridges;

public sealed class SosCartridgeSystem : EntitySystem
{
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SosCartridgeComponent, CartridgeActivatedEvent>(OnActivated);
    }

    private void OnActivated(EntityUid uid, SosCartridgeComponent component, CartridgeActivatedEvent args)
    {
        if (_timing.CurTime < component.NextTime)
            return;

        //Get the PDA
        if (!TryComp<PdaComponent>(args.Loader, out var pda))
            return;

        // Get the ID, if possible
        TryComp<IdCardComponent>(pda.ContainedId, out var id);
        var displayName = id?.FullName ?? component.LocalizedDefaultName;

        foreach (var channel in component.HelpChannels)
        {
            _radio.SendRadioMessage(uid, Loc.GetString(component.HelpMessage, ("user", displayName)), channel, uid);
        }

        component.NextTime = _timing.CurTime + component.Cooldown;
    }
}

