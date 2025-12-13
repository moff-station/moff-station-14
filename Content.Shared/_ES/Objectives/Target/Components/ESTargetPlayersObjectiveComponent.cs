using Robust.Shared.GameStates;

namespace Content.Shared._ES.Objectives.Target.Components;

/// <summary>
/// Works with <see cref="ESTargetObjectiveComponent"/> to select all living players as valid candidates.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESTargetPlayersObjectiveSystem))]
public sealed partial class ESTargetPlayersObjectiveComponent : Component;
