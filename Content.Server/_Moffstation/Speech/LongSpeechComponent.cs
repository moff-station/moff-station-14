using Robust.Shared.Audio;

namespace Content.Server._Moffstation.Speech;

[RegisterComponent]
public sealed partial class LongSpeechComponent : Component
{
    /// <summary>
    /// How many sounds are left in the currect speech
    /// </summary>
    [DataField]
    public int WordCount;

    /// <summary>
    /// The max amount of sounds that can be played in one speech
    /// </summary>
    [DataField]
    public TimeSpan MaxSpeechTime = TimeSpan.FromSeconds(2);

    [DataField]
    public ResolvedSoundSpecifier? Sound;

    [DataField]
    public AudioParams Params;

    [DataField]
    public TimeSpan Cooldown;

    [DataField]
    public TimeSpan NextSpeak;
    /// <summary>
    /// The pitch variation applied to the speech
    /// </summary>
    [DataField]
    public float PitchVariation = 0.1f;

    [DataField]
    public string Message;
}
