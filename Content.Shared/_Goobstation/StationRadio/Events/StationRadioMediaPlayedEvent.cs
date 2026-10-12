using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._Goobstation.StationRadio.Events; // Moffstation - _Goob -> _Goobstation

[Serializable, NetSerializable]
public sealed class StationRadioMediaPlayedEvent : EntityEventArgs
{
    public SoundPathSpecifier MediaPlayed { get; }
    public TimeSpan PlayOffset; // Moffstation - Add resume play.
    public StationRadioMediaPlayedEvent(SoundPathSpecifier Media, TimeSpan playOffset = default) // Moffstation - Add resume play.
    {
        MediaPlayed = Media;
        PlayOffset = playOffset; // Moffstation - Add resume play.
    }
}
