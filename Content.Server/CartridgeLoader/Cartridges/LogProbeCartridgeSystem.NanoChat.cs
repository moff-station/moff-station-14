using Content.Shared.Audio;
using Content.Shared._CD.CartridgeLoader.Cartridges;
using Content.Shared._CD.NanoChat;
using Robust.Shared.Random;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class LogProbeCartridgeSystem
{
    [Dependency] private IRobustRandom _random = default!;

    private void InitializeNanoChat()
    {
        SubscribeLocalEvent<NanoChatRecipientUpdatedEvent>(OnRecipientUpdated);
        SubscribeLocalEvent<NanoChatMessageReceivedEvent>(OnMessageReceived);
    }

    private void OnRecipientUpdated(ref NanoChatRecipientUpdatedEvent args)
    {
        foreach (var (ent, updateUi) in AllLogProbes())
        {
            if (ent.Comp.ScannedNanoChatData == null ||
                GetEntity(ent.Comp.ScannedNanoChatData.Value.Card) != args.CardUid)
                continue;

            if (!TryComp<NanoChatCardComponent>(args.CardUid, out var card))
                continue;

            ent.Comp.ScannedNanoChatData = new NanoChatData(
                new Dictionary<uint, NanoChatRecipient>(card.Recipients),
                ent.Comp.ScannedNanoChatData.Value.Messages,
                card.Number,
                GetNetEntity(args.CardUid));

            updateUi();
        }
    }

    private void OnMessageReceived(ref NanoChatMessageReceivedEvent args)
    {
        foreach (var (ent, updateUi) in AllLogProbes())
        {
            if (ent.Comp.ScannedNanoChatData == null ||
                GetEntity(ent.Comp.ScannedNanoChatData.Value.Card) != args.CardUid)
                continue;

            if (!TryComp<NanoChatCardComponent>(args.CardUid, out var card))
                continue;

            ent.Comp.ScannedNanoChatData = new NanoChatData(
                ent.Comp.ScannedNanoChatData.Value.Recipients,
                new Dictionary<uint, List<NanoChatMessage>>(card.Messages),
                card.Number,
                GetNetEntity(args.CardUid));

            updateUi();
        }
    }

    private void ScanNanoChatCard<T>(Entity<T> ent, EntityUid user, Entity<NanoChatCardComponent> target)
    where T : BaseLogProbeComponent // Moffstation - Split the component to be reusable
    {
        _audio.PlayEntity(ent.Comp.SoundScan,
            user,
            target,
            AudioHelpers.WithVariation(0.25f, _random));
        _popup.PopupCursor(Loc.GetString("log-probe-scan-nanochat", ("card", target)), user);

        ent.Comp.PulledAccessLogs.Clear();

        ent.Comp.ScannedNanoChatData = new NanoChatData(
            new Dictionary<uint, NanoChatRecipient>(target.Comp.Recipients),
            new Dictionary<uint, List<NanoChatMessage>>(target.Comp.Messages),
            target.Comp.Number,
            GetNetEntity(target)
        );
    }
}
