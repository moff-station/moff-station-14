using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.JestographicSequencer;

/// <summary>
///Added to an entity that has had a non-replacing special interaction applied, so it only works once.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class JestographicInteractedComponent : Component;
