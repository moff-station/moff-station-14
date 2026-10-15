using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Photography;

/// <summary>
/// This component enables cameras to record what the photographer could see.
/// The scene is recorded server-side; see <see cref="PhotoSnapshot"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SnapshotCameraComponent : Component
{
    /// <summary>
    /// The photograph entity that gets printed.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Photograph;

    /// <summary>
    /// Tiles from the middle of the shot to its edge gives a 9x7 tile photo.
    /// </summary>
    [DataField]
    public Vector2i HalfExtents = new(4, 3);

    /// <summary>
    /// How far from the photographer the middle of a shot may be. Covers the screen with the viewfinder's extended view.
    /// </summary>
    [DataField]
    public float MaxRange = 14f;

    /// <summary>
    /// Whether the holder is looking through the viewfinder. While they are, clicking takes a photo centered on the
    /// clicked spot instead of interacting.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Aiming;
}

/// <summary>
/// Raised on a camera when a shot has been taken, centered on <paramref name="Center"/>. The server records the photo.
/// </summary>
[ByRefEvent]
public readonly record struct SnapshotCameraShotEvent(EntityUid User, MapCoordinates Center);
