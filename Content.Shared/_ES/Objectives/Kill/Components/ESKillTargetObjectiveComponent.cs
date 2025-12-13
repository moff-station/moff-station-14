using Robust.Shared.GameStates;

namespace Content.Shared._ES.Objectives.Kill.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESKillTargetObjectiveSystem))]
public sealed partial class ESKillTargetObjectiveComponent : Component;
