using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Overlay.Components;

/// <summary>
/// When this component is on a player-controlled entity, it applies a night vision visual effect and creates a client-
/// only pointlight to illuminate the area around the entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NightVisionComponent : Component;
