using Content.Server.Chat.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.Chat;
using Content.Shared.PDA;
using Robust.Server.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.CartridgeLoader.Cartridges;

public sealed class SOSCartridgeSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SOSCartridgeComponent, CartridgeActivatedEvent>(OnActivated);
    }

    private void OnActivated(Entity<SOSCartridgeComponent> ent, ref CartridgeActivatedEvent args)
    {
        if (!ent.Comp.CanCall)
            return;

        //Get the PDA
        if (!TryComp<PdaComponent>(args.Loader, out var pda))
            return;

        //Get the id container
        if (!_container.TryGetContainer(args.Loader, SOSCartridgeComponent.PDAIdContainer, out var idContainer))
            return;

        //If theres nothing in id slot, send message anonymously
        if (idContainer.ContainedEntities.Count == 0)
        {
            _radio.SendRadioMessage(ent.Owner,
                Loc.GetString(ent.Comp.HelpMessage, ("name", ent.Comp.LocalizedDefaultName)),
                ent.Comp.HelpChannel,
                ent.Owner);
        }
        else
        {
            //Otherwise, send a message with the full name of every id in there
            foreach (var idCard in idContainer.ContainedEntities)
            {
                if (!TryComp<IdCardComponent>(idCard, out var idCardComp))
                    return;

                _radio.SendRadioMessage(ent.Owner,
                    Loc.GetString(ent.Comp.HelpMessage, ("name", idCardComp.FullName ?? ent.Comp.LocalizedDefaultName)),
                    ent.Comp.HelpChannel,
                    ent.Owner);
            }
        }
        _chat.TrySendInGameICMessage(args.Loader,
            Loc.GetString(ent.Comp.NotificationMessage),
            InGameICChatType.Speak,
            ChatTransmitRange.HideChat);

        ent.Comp.NextUse = _timing.CurTime + ent.Comp.Cooldown;
    }
}
