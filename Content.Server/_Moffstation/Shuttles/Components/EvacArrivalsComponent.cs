using Content.Server._Moffstation.Shuttles.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Moffstation.Shuttles.Components;

/// Put on the evac shuttle while it is bringing the crew to the station at round start.
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(EvacArrivalsSystem))]
public sealed partial class EvacArrivalsComponent : Component
{
    [DataField]
    public EntityUid Station;

    [DataField]
    public EvacArrivalsState State = EvacArrivalsState.InTransit;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? DepartTime;
}

public enum EvacArrivalsState : byte
{
    InTransit,
    Docked,
    Returning,
}
