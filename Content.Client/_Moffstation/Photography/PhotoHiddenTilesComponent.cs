using System.Numerics;

namespace Content.Client._Moffstation.Photography;

/// <summary>
/// tiles the photographer couldn't see, drawn black by <see cref="PhotoHiddenTilesOverlay"/>.
/// </summary>
[RegisterComponent]
public sealed partial class PhotoHiddenTilesComponent : Component
{
    [ViewVariables]
    public List<Vector2i> Tiles = new();

    /// <summary>
    /// Shadows cast by walls from the photographer's position, as a triangle list in grid-local coordinates.
    /// </summary>
    [ViewVariables]
    public Vector2[] Shadows = [];
}
