using Content.Shared._Moffstation.Trigger.Components.Effects;
using Content.Shared.Chat;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceLinking.Systems;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Moffstation.DeviceLinking.Systems;

public sealed partial class SignalSpeechSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private DeviceLinkSystem _links = default!;

    [SubscribeLocalEvent]
    private void Init(Entity<Components.SignalSpeechComponent> ent, ref MapInitEvent ev)
    {
        foreach (var port in _links.GetSinkPorts(ent.Owner))
        {
            if (port == ent.Comp.RandomPort)
                continue;

            if (! ent.Comp.Lines.TryGetValue(port, out _))
                ent.Comp.Lines.Add(port, string.Empty);
        }
    }

    [SubscribeLocalEvent]
    private void OnTrigger(Entity<Components.SignalSpeechComponent> ent, ref SignalReceivedEvent ev)
    {
        if (ent.Comp.Lines.TryGetValue(ev.Port, out var line))
        {
            _chat.TrySendInGameICMessage(ent,
                line,
                InGameICChatType.Speak,
                ChatTransmitRange.Normal,
                true);
        }
        else if (ev.Port == ent.Comp.RandomPort && ent.Comp.Lines.Count > 0)
        {
            _chat.TrySendInGameICMessage(ent,
                _random.Pick(ent.Comp.Lines.Values),
                InGameICChatType.Speak,
                ChatTransmitRange.Normal,
                true);
        }
    }

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<Components.SignalSpeechComponent> ent, ref GetVerbsEvent<AlternativeVerb> ev)
    {
        if (!ev.CanAccess || !ev.CanInteract)
            return;

        if (ent.Comp.RecordingPort != null)
        {
            ev.Verbs.Add(new AlternativeVerb
            {
                Text = "Stop recording",
                Act = () => StopRecording(ent, "cancelled"),
                Priority = 0,
            });
        }
        else
        {
            var category = new VerbCategory("Record", null);
            var i = 1;
            foreach (var port in ent.Comp.Lines.Keys)
            {
                ev.Verbs.Add(new AlternativeVerb
                {
                    Text = "voice line " + i,
                    Act = () => StartRecording(ent, port),
                    Priority = -i,
                    Category = category,
                });
                i++;
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnListen(Entity<Components.SignalSpeechComponent> ent, ref ListenEvent ev)
    {
        if (ev.Source == ent.Owner || ent.Comp.RecordingPort is not {} recorded)
            return;

        var line = ev.Message.Trim();

        var popupId = "success";
        if (line.Length >= ent.Comp.MinLength && line.Length <= ent.Comp.MaxLength)
            ent.Comp.Lines[recorded] = line;
        else if (line.Length < ent.Comp.MinLength)
            popupId = "too short";
        else if (line.Length > ent.Comp.MaxLength)
            popupId = "too long";

        StopRecording(ent, popupId);
    }

    private void StartRecording(Entity<Components.SignalSpeechComponent> ent, ProtoId<SinkPortPrototype> port)
    {
        _popup.PopupEntity("started", ent);
        Dirty(ent);
        EnsureComp<ActiveListenerComponent>(ent).Range = ent.Comp.RecordingRange;
        ent.Comp.RecordingPort = port;
    }

    private void StopRecording(Entity<Components.SignalSpeechComponent> ent, string popupId)
    {
        _popup.PopupEntity(popupId, ent);
        Dirty(ent);
        ent.Comp.RecordingPort = null;
        RemCompDeferred<ActiveListenerComponent>(ent);
    }
}
