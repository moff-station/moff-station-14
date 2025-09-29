using Content.Server.Speech;
using Content.Shared.Speech;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
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
        while (query.MoveNext(out var uid, out _, out var longSpeech))
        {
            if (longSpeech.NextSpeak > _gameTiming.CurTime)
                continue;

            if (longSpeech.WordCount <= 0)
            {
                RemCompDeferred<LongSpeechComponent>(uid);
                continue;
            }

            longSpeech.WordCount--;
            longSpeech.NextSpeak = _gameTiming.CurTime + longSpeech.Cooldown;
            _audio.PlayPvs(longSpeech.Sound, uid, longSpeech.Params);
        }
    }

    public void SpeakSentence(Entity<SpeechComponent> ent, string message)
    {
        //If we're already speaking, finish the current sentence.
        if (HasComp<LongSpeechComponent>(ent))
            return;

        if (_speechSound.GetSpeechSound(ent, message) is not { } sound)
            return;

        var longSpeech = EnsureComp<LongSpeechComponent>(ent);

        longSpeech.Params = sound.Params.WithVariation(longSpeech.PitchVariation);
        longSpeech.Sound = _audio.ResolveSound(sound);
        longSpeech.Cooldown = _audio.GetAudioLength(longSpeech.Sound);
        longSpeech.WordCount = message.Split(' ').Length;

        // Decrease the wordcount until it fits within the speechtime
        while (longSpeech.WordCount * longSpeech.Cooldown > longSpeech.MaxSpeechTime && longSpeech.WordCount > 1)
        {
            longSpeech.WordCount--;
        }
    }
}
