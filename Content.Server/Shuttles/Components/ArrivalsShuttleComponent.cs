using Content.Server.Shuttles.Systems;
using Robust.Shared.Serialization;  // Moffstation
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Shuttles.Components;

[RegisterComponent, Access(typeof(ArrivalsSystem)), AutoGenerateComponentPause]
public sealed partial class ArrivalsShuttleComponent : Component
{
    [DataField("station")]
    public EntityUid Station;

    [DataField("nextTransfer", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextTransfer;

    [DataField("nextArrivalsTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextArrivalsTime;

    // Moffstation - Start - Keep track of the first time hitting the station
    /// <summary>
    /// True until the arrivals shuttle arrives at the station for the first time
    /// </summary>
    [DataField]
    public bool FirstArrival = true;
    // Moffstation - End

    /// <summary>
    ///     the first arrivals FTL originates from nullspace instead of the station
    /// </summary>
    [DataField("firstRun")]
    public bool FirstRun = true;

}

// Moffstation - Start - First arrivals event
/// <summary>
///     Event for the first time the arrivals shuttle reaches the station.
/// </summary>
[Serializable, NetSerializable]
public sealed class FirstArrivalEvent : EventArgs;
// Moffstation - End
