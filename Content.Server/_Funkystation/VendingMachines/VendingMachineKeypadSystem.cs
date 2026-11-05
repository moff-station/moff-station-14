using Content.Shared._Funkystation.VendingMachines;
using Content.Shared.Timing.Systems;
using Content.Shared.VendingMachines;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Funkystation.VendingMachines;

public sealed partial class VendingMachineKeypadSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = null!;
    [Dependency] private UseDelaySystem _useDelay = null!;

    private const string KeypadSoundDelayId = "keypadSound";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VendingMachineComponent, VendingMachineKeypadAudioMessage>(OnKeypadAudio);
    }

    private void OnKeypadAudio(EntityUid uid, VendingMachineComponent component, VendingMachineKeypadAudioMessage args)
    {
        _useDelay.SetLength(uid, component.KeypadSoundCooldown, KeypadSoundDelayId);
        if (!_useDelay.TryResetDelay(uid, true, KeypadSoundDelayId))
            return;

        var sound = args.SoundType switch
        {
            VendingMachineKeypadSound.Beep => component.BeepSound,
            VendingMachineKeypadSound.Success => component.SuccessSound,
            VendingMachineKeypadSound.Error => component.ErrorSound,
            VendingMachineKeypadSound.Timeout => component.TimeoutSound,
            _ => component.BeepSound,
        };

        var audioParams = sound.Params.WithPitchScale(args.Pitch);

        _audio.PlayPredicted(sound, uid, args.Actor, audioParams);
    }
}
