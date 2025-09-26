using Robust.Shared.Audio;

namespace Content.Server._Moffstation.Speech;

[RegisterComponent]
public sealed partial class LongSpeechComponent : Component
{
    [DataField]
    public int SyllablesLeft;

    [DataField]
    public int MaxSyllables = 10;

    [DataField]
    public SoundSpecifier Sound;

    [DataField]
    public float PitchVariation = 0.2f;

    [DataField]
    public string Message;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(0.2f);

    [DataField]
    public float TimeVariation = 0.05f;

    [DataField]
    public TimeSpan NextSpeak;
}
