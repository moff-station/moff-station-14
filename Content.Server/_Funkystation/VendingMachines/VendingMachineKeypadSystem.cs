using Content.Shared._Funkystation.VendingMachines;
using Content.Shared.VendingMachines;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Funkystation.VendingMachines;

public sealed partial class VendingMachineKeypadSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = null!;
    [Dependency] private IGameTiming _timing = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VendingMachineComponent, VendingMachineKeypadAudioMessage>(OnKeypadAudio);
    }

    private void OnKeypadAudio(EntityUid uid, VendingMachineComponent component, VendingMachineKeypadAudioMessage args)
    {
        if (_timing.CurTime < component.NextKeypadSound)
            return;

        component.NextKeypadSound = _timing.CurTime + component.KeypadSoundCooldown;

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
