using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._Funkystation.Footprints;

[Serializable, NetSerializable]
public sealed class FootprintStateEvent(NetEntity netEntity) : EntityEventArgs
{
    public NetEntity NetEntity = netEntity;
}

[ByRefEvent]
public readonly record struct FootprintCleanEvent(SoundSpecifier Sound); // Moff - tack on sounds
