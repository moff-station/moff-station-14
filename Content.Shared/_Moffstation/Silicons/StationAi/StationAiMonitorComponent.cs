using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Silicons.StationAi;

/// <summary>
/// Allow the player to use a Station Ai Monitor
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StationAiMonitorComponent : Component
{
    /// <summary>
    /// Minimum delay between two consecutive warps, in seconds.
    /// </summary>
    [DataField]
    public float DelayBetweenWarps = 0.5f;

    public TimeSpan NextWarpAllowed = TimeSpan.Zero;

    public Dictionary<string, SuitSensorStatus> ConnectedSensors = new();
}
