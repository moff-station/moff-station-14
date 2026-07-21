using Robust.Shared.GameStates;

namespace Content.Shared._Funkystation.Clothing.Components;

/// <summary>
/// For items that should darken the screen when equipped.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WeldingMaskOverlayComponent : Component
{
    /// <summary>
    /// Path to the texture used for the screen overlay.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ResPath Texture = new("/Textures/_Funkystation/Clothing/Head/Welding/weldingOverlay.png");
}
