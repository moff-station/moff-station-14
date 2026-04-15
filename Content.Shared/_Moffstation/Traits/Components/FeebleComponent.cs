using Content.Shared.Weapons.Melee;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Traits.Components;

// Like to the DisarmProne Component, but used as a trait rather than a punishment.
[RegisterComponent, NetworkedComponent]
public sealed partial class FeebleComponent : Component;
