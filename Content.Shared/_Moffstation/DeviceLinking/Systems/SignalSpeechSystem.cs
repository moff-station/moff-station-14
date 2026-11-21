using Content.Shared._Moffstation.DeviceLinking.Components;
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
    private void OnStartup(Entity<SignalSpeechComponent> ent, ref ComponentStartup ev)
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
    private void OnTrigger(Entity<SignalSpeechComponent> ent, ref SignalReceivedEvent ev)
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
    private void OnGetVerbs(Entity<SignalSpeechComponent> ent, ref GetVerbsEvent<AlternativeVerb> ev)
    {
        if (!ev.CanAccess || !ev.CanInteract)
            return;

        if (ent.Comp.RecordingPort != null)
        {
            ev.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("voice-emitter-stop"),
                Act = () => StopRecording(ent, Loc.GetString("voice-emitter-stop-recording")),
                Priority = 0,
            });
        }
        else
        {
            var category = new VerbCategory(Loc.GetString("voice-emitter-record"), null);
            var i = 1;
            foreach (var port in ent.Comp.Lines.Keys)
            {
                ev.Verbs.Add(new AlternativeVerb
                {
                    Text = Loc.GetString("voice-emitter-record-line", ("index", i)),
                    Act = () => StartRecording(ent, port),
                    Priority = -i,
                    Category = category,
                });
                i++;
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnListen(Entity<SignalSpeechComponent> ent, ref ListenEvent ev)
    {
        if (ev.Source == ent.Owner || ent.Comp.RecordingPort is not {} recorded)
            return;

        var line = ev.Message.Trim();

        var popupId = Loc.GetString("voice-emitter-recorded");
        if (line.Length >= ent.Comp.MinLength && line.Length <= ent.Comp.MaxLength)
            ent.Comp.Lines[recorded] = line;
        else if (line.Length < ent.Comp.MinLength)
            popupId = Loc.GetString("voice-emitter-failed-record-failed-too-short");
        else if (line.Length > ent.Comp.MaxLength)
            popupId = Loc.GetString("voice-emitter-record-failed-too-long");

        StopRecording(ent, popupId);
    }

    private void StartRecording(Entity<SignalSpeechComponent> ent, ProtoId<SinkPortPrototype> port)
    {
        _popup.PopupEntity(Loc.GetString("voice-emitter-start-recording"), ent);
        Dirty(ent);
        EnsureComp<ActiveListenerComponent>(ent).Range = ent.Comp.RecordingRange;
        ent.Comp.RecordingPort = port;
    }

    private void StopRecording(Entity<SignalSpeechComponent> ent, string popupId)
    {
        _popup.PopupEntity(popupId, ent);
        Dirty(ent);
        ent.Comp.RecordingPort = null;
        RemCompDeferred<ActiveListenerComponent>(ent);
    }
}
