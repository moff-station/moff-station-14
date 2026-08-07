using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._Goobstation.StationRadio.Events; // Moffstation - _Goob -> _Goobstation

[Serializable, NetSerializable]
public sealed class StationRadioMediaPlayedEvent : EntityEventArgs
{
    public SoundPathSpecifier MediaPlayed { get; }
    public StationRadioMediaPlayedEvent(SoundPathSpecifier Media)
    {
        MediaPlayed = Media;
    }
}
