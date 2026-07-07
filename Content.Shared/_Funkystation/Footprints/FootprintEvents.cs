using Robust.Shared.Serialization;

namespace Content.Shared._Funkystation.Footprints;

[Serializable, NetSerializable]
public sealed class FootprintStateEvent : EntityEventArgs
{
    public NetEntity NetEntity;
    public FootprintStateEvent(NetEntity netEntity)
    {
        NetEntity = netEntity;
    }
}

[ByRefEvent]
public readonly record struct FootprintCleanEvent();
