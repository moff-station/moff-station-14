using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Overlay.Components;

/// <summary>
///     Apply a shader to your vision.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShowShaderComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Shader = "Goggles";

    [DataField, AutoNetworkedField]
    public Color Color = Color.FromHex("#5AB43CCC");
}
