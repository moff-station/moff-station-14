using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Interaction;

/// <summary>
/// This marker component causes interaction particles to not be generated when interacting with its owner.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MoffDisallowInteractionParticlesComponent : Component;
