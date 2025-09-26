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
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SpeechComponent, LongSpeechComponent>();
        while (query.MoveNext(out var uid, out _, out var longSpeech))
        {
            if (longSpeech.NextSpeak > _gameTiming.CurTime)
                continue;

            if (longSpeech.SyllablesLeft <= 0)
            {
                RemCompDeferred<LongSpeechComponent>(uid);
                continue;
            }

            _audio.PlayPvs(longSpeech.Sound, uid);

            longSpeech.SyllablesLeft--;
            longSpeech.NextSpeak = _gameTiming.CurTime +
                                   longSpeech.Cooldown +
                                   TimeSpan.FromSeconds(_random.NextFloat(-longSpeech.TimeVariation, longSpeech.TimeVariation));
        }
    }

    public void SpeakSentence(Entity<SpeechComponent> ent, string message)
    {
        //If we're already speaking, finish the current sentence.
        if (HasComp<LongSpeechComponent>(ent))
            return;

        var longSpeech = EnsureComp<LongSpeechComponent>(ent);

        if (_speechSound.GetSpeechSound(ent, message) is not { } sound)
        {
            RemCompDeferred<LongSpeechComponent>(ent);
            return;
        }
        longSpeech.Sound = sound;
        longSpeech.Sound.Params = longSpeech.Sound.Params with { Variation = longSpeech.PitchVariation };

        if (ent.Comp.LongSpeechCooldown != null)
            longSpeech.Cooldown = ent.Comp.LongSpeechCooldown.Value;


        longSpeech.SyllablesLeft = Math.Min(message.Split(' ').Length, longSpeech.MaxSyllables);

    }
}
