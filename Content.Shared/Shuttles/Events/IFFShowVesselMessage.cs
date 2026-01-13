// Moffstation - Start - Revert IFF changes
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

[Serializable, NetSerializable]
public sealed class IFFShowVesselMessage : BoundUserInterfaceMessage
{
    public bool Show;
}
// Moffstation - End
