using Robust.Shared.Audio;

namespace Content.Server._Moffstation.Speech;

[RegisterComponent]
public sealed partial class LongSpeechComponent : Component
{
    /// <summary>
    /// How many sounds are left in the currect speech
    /// </summary>
    [DataField]
    public int SyllablesLeft;

    /// <summary>
    /// The max amount of sounds that can be played in one speech
    /// </summary>
    [DataField]
    public int MaxSyllables = 10;

    [DataField]
    public SoundSpecifier Sound;

    /// <summary>
    /// The pitch variation applied to the speech
    /// </summary>
    [DataField]
    public float PitchVariation = 0.05f;

    [DataField]
    public string Message;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(0.2f);

    /// <summary>
    /// The time variation in seconds on the cooldown
    /// </summary>
    [DataField]
    public float TimeVariation;

    [DataField]
    public TimeSpan NextSpeak;
}
