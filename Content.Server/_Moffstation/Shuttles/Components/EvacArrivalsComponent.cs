using Content.Server._Moffstation.Shuttles.Systems;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Moffstation.Shuttles.Components;

/// Put on an evac shuttle while it is bringing the crew to the station at round start.
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(EvacArrivalsSystem))]
public sealed partial class EvacArrivalsComponent : Component
{
    [DataField]
    public EntityUid Station;

    /// Where the shuttle returns to after dropping off the crew.
    [DataField]
    public EntityCoordinates Origin;

    [DataField]
    public Angle OriginRotation;

    [DataField]
    public EvacArrivalsState State = EvacArrivalsState.InTransit;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? DepartTime;

    /// The shuttle's own FTL cooldown override, restored once the arrival is over.
    [DataField]
    public TimeSpan? CooldownOverride;
}

public enum EvacArrivalsState : byte
{
    InTransit,
    Docked,
    Returning,
}

/// Raised on the emergency shuttle when it FTLs; cancels if the trip isn't the evac departure that ends the round.
[ByRefEvent]
public record struct EmergencyShuttleEvacDepartureCheckEvent(bool Cancelled = false);
