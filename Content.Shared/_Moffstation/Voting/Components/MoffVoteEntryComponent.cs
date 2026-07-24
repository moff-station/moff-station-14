using Content.Shared._Moffstation.Voting.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Voting.Components;

/// <summary>
/// This is used for marking things to go in the ES voting panel, even if they're not explicitly votes
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(MoffVoteEntrySystem))]
public sealed partial class MoffVoteEntryComponent : Component;
