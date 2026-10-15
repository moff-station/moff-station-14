using System.Numerics;
using Content.Shared._Moffstation.Photography;
using Content.Shared.Hands.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client._Moffstation.Photography;

/// <summary>
/// While the local player looks through a camera's viewfinder, frames the area a photo taken at the mouse would cover.
/// Everything outside the frame is dimmed, and the frame turns red when the spot is out of range.
/// </summary>
public sealed partial class PhotoViewfinderOverlay : Robust.Client.Graphics.Overlay
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IEyeManager _eye = default!;

    private readonly SharedHandsSystem _hands;
    private readonly SharedTransformSystem _transform;
    private readonly SnapshotCameraSystem _camera;

    private static readonly Color FrameColor = Color.White.WithAlpha(0.9f);
    private static readonly Color OutOfRangeColor = Color.Red.WithAlpha(0.9f);
    private static readonly Color DimColor = Color.Black.WithAlpha(0.35f);

    private const float LineWidth = 0.06f;
    private const float CornerLength = 1f;
    private const float CrosshairLength = 0.25f;

    /// <summary>
    /// How far the dimming reaches past the frame, in tiles.
    /// </summary>
    private const float DimReach = 64f;

    private Entity<SnapshotCameraComponent>? _aimed;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public PhotoViewfinderOverlay()
    {
        IoCManager.InjectDependencies(this);
        _hands = _entityManager.System<SharedHandsSystem>();
        _transform = _entityManager.System<SharedTransformSystem>();
        _camera = _entityManager.System<SnapshotCameraSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _aimed = null;
        if (_player.LocalEntity is not { } player
            || _hands.GetActiveItem(player) is not { } item
            || !_entityManager.TryGetComponent<SnapshotCameraComponent>(item, out var camera)
            || !camera.Aiming)
        {
            return false;
        }

        _aimed = (item, camera);
        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_aimed is not { } camera || _player.LocalEntity is not { } player)
            return;

        var mouse = _eye.PixelToMap(_input.MouseScreenPosition);
        if (mouse.MapId != args.MapId)
            return;

        // Same clamping the shot uses, so the frame is where the photo will be.
        var center = _camera.ClampToScreen(mouse, camera.Comp.HalfExtents);

        var from = _transform.GetMapCoordinates(player);
        var inRange = from.MapId == center.MapId
                      && (center.Position - from.Position).Length() <= camera.Comp.MaxRange;
        var color = inRange ? FrameColor : OutOfRangeColor;

        var frame = _camera.GetFrame(center, camera.Comp.HalfExtents);
        var box = frame.Box;
        var handle = args.WorldHandle;
        handle.SetTransform(frame.FrameToWorld);

        // Dim around the frame, like the edge of a viewfinder.
        handle.DrawRect(new Box2(box.Left - DimReach, box.Top, box.Right + DimReach, box.Top + DimReach), DimColor);
        handle.DrawRect(new Box2(box.Left - DimReach, box.Bottom - DimReach, box.Right + DimReach, box.Bottom), DimColor);
        handle.DrawRect(new Box2(box.Left - DimReach, box.Bottom, box.Left, box.Top), DimColor);
        handle.DrawRect(new Box2(box.Right, box.Bottom, box.Right + DimReach, box.Top), DimColor);

        handle.DrawRect(box, color.WithAlpha(0.3f), filled: false);
        DrawCorner(handle, box.BottomLeft, new Vector2(1, 1), color);
        DrawCorner(handle, box.BottomRight, new Vector2(-1, 1), color);
        DrawCorner(handle, box.TopLeft, new Vector2(1, -1), color);
        DrawCorner(handle, box.TopRight, new Vector2(-1, -1), color);

        var middle = frame.CenterTile + new Vector2(0.5f);
        handle.DrawRect(Box2.CenteredAround(middle, new Vector2(CrosshairLength * 2, LineWidth)), color);
        handle.DrawRect(Box2.CenteredAround(middle, new Vector2(LineWidth, CrosshairLength * 2)), color);

        handle.SetTransform(Matrix3x2.Identity);
    }

    /// <summary>
    /// Draws an L-shaped bracket at the corners to Construct the viewfinder/>.
    /// </summary>
    private static void DrawCorner(DrawingHandleWorld handle, Vector2 corner, Vector2 inward, Color color)
    {
        var horizontalEnd = corner + new Vector2(inward.X * CornerLength, inward.Y * LineWidth);
        var verticalEnd = corner + new Vector2(inward.X * LineWidth, inward.Y * CornerLength);
        handle.DrawRect(Box2.FromTwoPoints(corner, horizontalEnd), color);
        handle.DrawRect(Box2.FromTwoPoints(corner, verticalEnd), color);
    }
}
