using Robust.Shared.Audio;

namespace Content.Server._Moffstation.Speech;

[RegisterComponent]
public sealed partial class LongSpeechComponent : Component
{
    [DataField]
    public int SyllablesLeft;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public string Message;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(0.2f);

    [DataField]
    public TimeSpan Variation = TimeSpan.FromSeconds(0.05f);

    [DataField]
    public TimeSpan NextSpeak;
}
