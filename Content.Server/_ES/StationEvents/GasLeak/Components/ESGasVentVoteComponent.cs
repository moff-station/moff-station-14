using Content.Shared._ES.Voting.Components;

namespace Content.Server._ES.StationEvents.GasLeak.Components;

/// <summary>
/// <see cref="ESVoteComponent"/> for a random gas vent on the station.
/// </summary>
[RegisterComponent]
[Access(typeof(ESGasLeakRule))]
public sealed partial class ESGasVentVoteComponent : Component
{
    [DataField]
    public int Count = 6;
}
