using Robust.Shared.GameStates;
using Content.Shared.Toggleable; // Moffstation

namespace Content.Shared.Light.Components;

/// <summary>
/// Toggles point light on an entity whenever ItemToggle hits.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ItemTogglePointLightComponent : Component
{
    // Moffstation
    /// <summary>
    /// When true, causes the color specified in <see cref="ToggleVisuals.Color"/>
    /// be used to modulate the color of lights on this entity.
    /// </summary>
    [DataField("Moffstation_ToggleVisualsColorModulatesLights")]
    public bool ToggleVisualsColorModulatesLights = false;
}
