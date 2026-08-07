using Content.Shared._Goobstation.StationRadio.Components; // Moffstation - _Goob -> _Goobstation
using Content.Shared._Goobstation.StationRadio.Events; // Moffstation - _Goob -> _Goobstation
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Audio.Systems;
using Content.Shared.DeviceLinking; // Moffstation - Move Station Radio Server Check to StationRadioReceiverSystem
using Content.Shared.Radio.Components; // Moffstation - Alt Click to Lower Volume.
using Content.Shared.Verbs; // Moffstation - Alt Click to Lower Volume.

namespace Content.Shared._Goobstation.StationRadio.Systems; // Moffstation - _Goob -> _Goobstation

public sealed partial class StationRadioReceiverSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationRadioReceiverComponent, StationRadioMediaPlayedEvent>(OnMediaPlayed);
        SubscribeLocalEvent<StationRadioReceiverComponent, StationRadioMediaStoppedEvent>(OnMediaStopped);
        SubscribeLocalEvent<StationRadioReceiverComponent, ActivateInWorldEvent>(OnRadioToggle);
        SubscribeLocalEvent<StationRadioReceiverComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<StationRadioServerComponent, EntityTerminatingEvent>(OnServerTerminating); // Moffstation - When Server is destroyed, it should stop broadcasting.
        SubscribeLocalEvent<RadioRigComponent, EntityTerminatingEvent>(OnRigTerminating); // Moffstation - When Rig is destroyed, it should stop broadcasting.

        SubscribeLocalEvent<StationRadioServerComponent, PowerChangedEvent>(OnServerPowerChanged); // Moffstation - If StationRadioServer has no power, broadcast stops.

        SubscribeLocalEvent<StationRadioReceiverComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs); // Moffstation - Alt click to lower volume.
    }

    private void OnPowerChanged(EntityUid uid, StationRadioReceiverComponent comp, PowerChangedEvent args)
    {
        if(comp.SoundEntity == null)
            return;
        _audio.SetGain(comp.SoundEntity, GetGain(comp, args.Powered));
    }

    private void OnRadioToggle(EntityUid uid, StationRadioReceiverComponent comp, ActivateInWorldEvent args)
    {
        comp.Active = !comp.Active;
        Dirty(uid, comp);
        if (comp.SoundEntity != null)
            _audio.SetGain(comp.SoundEntity, GetGain(comp, _power.IsPowered(uid)));
    }

    private void OnMediaPlayed(EntityUid uid, StationRadioReceiverComponent comp, StationRadioMediaPlayedEvent args)
    {
        var sound = _audio.PlayPvs(args.MediaPlayed, uid, comp.DefaultParams.WithVolume(-100f));
        if (sound == null)
            return;

        comp.SoundEntity = sound.Value.Entity;
            _audio.SetGain(comp.SoundEntity, GetGain(comp, _power.IsPowered(uid)));
    }

    private void OnMediaStopped(EntityUid uid, StationRadioReceiverComponent comp, StationRadioMediaStoppedEvent args)
    {
        if (comp.SoundEntity == null)
            return;

        comp.SoundEntity = _audio.Stop(comp.SoundEntity);
    }

    // Moffstation - Start

    /// <summary>
    /// Method for getting the current volume of the station radio.
    /// </summary>
    private static float GetGain(StationRadioReceiverComponent comp, bool powered)
    {
        if (!comp.Active || !powered)
            return 0f;

        return comp.LowVolume ? 0.1f : 1f;
    }

    /// <summary>
    /// Alt Click / Context Menu Verb for turning down the volume of the radio.
    /// </summary>
    private void OnGetAltVerbs(EntityUid uid, StationRadioReceiverComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = comp.LowVolume ? "station-radio-receiver-increase-volume" : "station-radio-receiver-decrease-volume",
            Act = () =>
            {
                if (TryComp<RadioSpeakerComponent>(uid, out var speaker))
                {
                    speaker.LouderSpeech = !speaker.LouderSpeech;
                    Dirty(uid, speaker);
                }
                comp.LowVolume = !comp.LowVolume;
                Dirty(uid, comp);
                if (comp.SoundEntity != null)
                    _audio.SetGain(comp.SoundEntity, GetGain(comp, _power.IsPowered(uid)));
            }
        });
    }

    /// <summary>
    /// Resolves whether Radio Rig is connected to a Radio Server that has power,
    /// and whether or not it can broadcast.
    /// </summary>
    public bool TryGetLinkedPoweredServer(EntityUid uid, out EntityUid server)
    {
        server = default;

        if (!TryComp<DeviceLinkSourceComponent>(uid, out var source))
            return false;

        foreach (var linkedRig in source.LinkedPorts.Keys)
        {
            if (!HasComp<RadioRigComponent>(linkedRig) || !TryComp<DeviceLinkSinkComponent>(linkedRig, out var sink))
                continue;

            foreach (var linkedServer in sink.LinkedSources)
            {
                var hasComp = HasComp<StationRadioServerComponent>(linkedServer);
                var powered = _power.IsPowered(linkedServer);

                if (!hasComp || !powered)
                    continue;

                server = linkedServer;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Stop broadcasting if the Radio Server loses power, despite the Vinyl Player and Rig still being powered.
    /// </summary>
    private void OnServerPowerChanged(EntityUid uid, StationRadioServerComponent comp, PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        var query = EntityQueryEnumerator<StationRadioReceiverComponent>();
        while (query.MoveNext(out var receiver, out _))
        {
            RaiseLocalEvent(receiver, new StationRadioMediaStoppedEvent());
        }
    }

    /// <summary>
    /// When the Radio Server is destroyed, stop all station radio receivers.
    /// </summary>
    private void OnServerTerminating(EntityUid uid, StationRadioServerComponent comp, ref EntityTerminatingEvent args) => StopAllReceivers();

    /// <summary>
    /// When the Radio Rig is destroyed, stop all station radio receivers.
    /// </summary>
    private void OnRigTerminating(EntityUid uid, RadioRigComponent comp, ref EntityTerminatingEvent args) => StopAllReceivers();

    /// <summary>
    /// Stop the broadcast on server destruction.
    /// </summary>
    private void StopAllReceivers()
    {
        var query = EntityQueryEnumerator<StationRadioReceiverComponent>();
        while (query.MoveNext(out var receiver, out _))
        {
            RaiseLocalEvent(receiver, new StationRadioMediaStoppedEvent());
        }
    }
    // Moffstation - End
}
