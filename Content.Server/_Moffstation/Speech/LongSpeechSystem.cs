using Content.Server.Speech;
using Content.Shared.Speech;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Speech;

public sealed class LongSpeechSystem : EntitySystem
{
    [Dependency] private readonly SpeechSoundSystem _speechSound = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SpeechComponent, LongSpeechComponent>();
        while (query.MoveNext(out var uid, out var speech, out var longSpeech))
        {
            if (longSpeech.NextSpeak > _gameTiming.CurTime)
                continue;

            if (longSpeech.SyllablesLeft < 1)
            {
                RemCompDeferred<LongSpeechComponent>(uid);
                continue;
            }

            longSpeech.Sound ??= _speechSound.GetSpeechSound((uid, speech), longSpeech.Message);
            _audio.PlayPvs(longSpeech.Sound, uid);
            longSpeech.SyllablesLeft -= 1;
            longSpeech.NextSpeak =  _gameTiming.CurTime + longSpeech.Cooldown;
        }
    }
}
