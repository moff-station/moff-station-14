using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._Moffstation.Photography;

/// <summary>
/// Draws what a photo's photographer couldn't see black,
/// Only photo grids have <see cref="PhotoHiddenTilesComponent"/>, so this draws nothing in the normal view.
/// </summary>
public sealed partial class PhotoHiddenTilesOverlay : Robust.Client.Graphics.Overlay
{
    [Dependency] private IEntityManager _entityManager = default!;

    private readonly SharedTransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public PhotoHiddenTilesOverlay()
    {
        IoCManager.InjectDependencies(this);
        _transform = _entityManager.System<SharedTransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var query = _entityManager.EntityQueryEnumerator<PhotoHiddenTilesComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var hidden, out var xform))
        {
            if (xform.MapID != args.MapId)
                continue;

            handle.SetTransform(_transform.GetWorldMatrix(xform));
            if (hidden.Shadows.Length > 0)
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, hidden.Shadows, Color.Black);

            foreach (var tile in hidden.Tiles)
            {
                handle.DrawRect(Box2.FromDimensions(tile, Vector2.One), Color.Black);
            }
        }

        handle.SetTransform(Matrix3x2.Identity);
    }
}
