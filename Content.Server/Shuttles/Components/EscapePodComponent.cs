using Content.Server.Shuttles.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// If added to a grid gets launched when the emergency shuttle launches.
/// </summary>
[RegisterComponent, Access(typeof(EmergencyShuttleSystem)), AutoGenerateComponentPause]
public sealed partial class EscapePodComponent : Component
{
    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan? LaunchTime;

    // Moffstation - Start - Allows detection if something else is queued to be docked
    [DataField]
    public ProtoId<TagPrototype> PriorityTag = "DockEmergencyPod";
    // Moffstation - End
}
