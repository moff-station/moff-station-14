using Content.Server._Moffstation.Shuttles.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Moffstation.Shuttles.Components;

/// Put on the evac shuttle while it is bringing the crew to the station at round start.
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(MoffTransitShuttleSystem))]
public sealed partial class MoffTransitShuttleComponent : Component
{
    [DataField]
    public EntityUid Station;

    [DataField]
    public TransitShuttleState State = TransitShuttleState.InTransit;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? DepartTime;
}

public enum TransitShuttleState : byte
{
    InTransit,
    Docked,
    Returning,
}
