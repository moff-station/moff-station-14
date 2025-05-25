using Content.Server._Moffstation.Spawners.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Spawners.Components;

/// <summary>
/// Spawns a prototype entity when an emergency shuttle is launched.
/// </summary>
[RegisterComponent, Access(typeof(SpawnOnEmergencyShuttleLaunchSystem))]
public sealed partial class SpawnOnEmergencyShuttleLaunchComponent : Component
{
    /// <summary>
    /// Entity prototype to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype = string.Empty;
}
