using Content.Server.Chat.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.Chat;
using Content.Shared.PDA;
using Content.Shared.Radio;
using Robust.Server.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.CartridgeLoader.Cartridges;

public sealed partial class SOSCartridgeSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SOSCartridgeComponent, CartridgeActivatedEvent>(OnActivated);
    }

    private void OnActivated(Entity<SOSCartridgeComponent> ent, ref CartridgeActivatedEvent args)
    {
        if (ent.Comp.NextUse >= _timing.CurTime)
            return;

        //Make sure it's a PDA
        if (!HasComp<PdaComponent>(args.Loader))
            return;

        //Get the id container
        if (!_container.TryGetContainer(args.Loader, SOSCartridgeComponent.PDAIdContainer, out var idContainer))
            return;

        //If there's nothing in id slot, send message with unknown name
        if (idContainer.ContainedEntities.Count == 0)
        {
            SendSoSMessage(args.Loader,
                ent.Comp.HelpChannel,
                Loc.GetString(ent.Comp.HelpMessage, ("name", ent.Comp.LocalizedDefaultName)),
                ent.Comp.LocalizedNotificationMessage);
        }
        else
        {
            //Otherwise, send a message with the full name of every id in there
            foreach (var idCard in idContainer.ContainedEntities)
            {
                if (!TryComp<IdCardComponent>(idCard, out var idCardComp))
                    return;

                if (_random.Prob(ent.Comp.FailChance) ||
                    !_radio.HasActiveServer(Transform(ent.Owner).MapID, ent.Comp.HelpChannel))
                {
                    _chat.TrySendInGameICMessage(args.Loader,
                        ent.Comp.LocalizedFailureNotificationMessage,
                        InGameICChatType.Speak,
                        ChatTransmitRange.HideChat);
                }
                else
                {
                    SendSoSMessage(args.Loader,
                        ent.Comp.HelpChannel,
                        Loc.GetString(ent.Comp.HelpMessage, ("name", idCardComp.FullName ?? ent.Comp.LocalizedDefaultName)),
                        Loc.GetString(_random.Prob(ent.Comp.FunnyChance)
                            ? ent.Comp.LocalizedFunnyNotificationMessage
                            : ent.Comp.LocalizedNotificationMessage));
                }
            }
        }

        ent.Comp.NextUse = _timing.CurTime + ent.Comp.Cooldown;
    }

    private void SendSoSMessage(EntityUid speaker, ProtoId<RadioChannelPrototype> radioChannel, string radioMessage, string? localMessage)
    {
        _radio.SendRadioMessage(speaker,
            radioMessage,
            radioChannel,
            speaker);

        if (!string.IsNullOrEmpty(localMessage))
        {
            _chat.TrySendInGameICMessage(speaker,
                localMessage,
                InGameICChatType.Speak,
                ChatTransmitRange.HideChat);
        }
    }
}
