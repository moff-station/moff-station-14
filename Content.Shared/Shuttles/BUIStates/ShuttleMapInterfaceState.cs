using Content.Shared.Shuttles.Systems;
using Content.Shared.Shuttles.UI.MapObjects;
using Content.Shared.Timing;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

/// <summary>
/// Handles BUI data for Map screen.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShuttleMapInterfaceState
{
    /// <summary>
    /// The current FTL state.
    /// </summary>
    public readonly FTLState FTLState;

    /// <summary>
    /// When the current FTL state starts and ends.
    /// </summary>
    public StartEndTime FTLTime;

    public List<ShuttleBeaconObject> Destinations;

    public List<ShuttleExclusionObject> Exclusions;

    // Moff Start - Exit Sector button
    /// <summary>
    /// Whether the console this state came from offers the exit sector button.
    /// </summary>
    public bool CanExitSector;
    // Moff end

    public ShuttleMapInterfaceState(
        FTLState ftlState,
        StartEndTime ftlTime,
        List<ShuttleBeaconObject> destinations,
        List<ShuttleExclusionObject> exclusions)
    {
        FTLState = ftlState;
        FTLTime = ftlTime;
        Destinations = destinations;
        Exclusions = exclusions;
    }
}
