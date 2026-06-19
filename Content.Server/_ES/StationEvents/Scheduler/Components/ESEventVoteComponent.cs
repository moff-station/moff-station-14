using Content.Shared._ES.Voting.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.StationEvents.Scheduler.Components;

/// <summary>
/// Variant of <see cref="ESVoteComponent"/> that interfaces with <see cref="ESEventVoteSchedulerComponent"/>
/// to create votes for station events.
/// </summary>
[RegisterComponent]
[Access(typeof(ESEventVoteSchedulerSystem))]
public sealed partial class ESEventVoteComponent : Component
{
    [DataField]
    public List<EntProtoId> EventIds = new();
}
