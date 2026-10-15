using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Client.MapText;
using Content.Shared._Moffstation.Photography;
using Content.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Parallax;
using Content.Shared.Sprite;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Moffstation.Photography;

/// <summary>
/// Rebuilds a photograph's snapshot on a client-only map so it can be looked at through a fixed eye.
/// The map exists only on this client <see cref="PhotoBoundUserInterface"/> and deletes it when the photo is closed.
/// </summary>
public sealed partial class PhotoViewerSystem : EntitySystem
{
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private SharedRoofSystem _roof = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private SharedScaleVisualsSystem _scaleVisuals = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private ITileDefinitionManager _tileDef = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    private static readonly Vector2 Center = new(0.5f, 0.5f);

    private static readonly TimeSpan RequestRetryDelay = TimeSpan.FromSeconds(0.2);
    private const int MaxRequests = 25;

    private readonly List<(PhotoBoundUserInterface Ui, PhotoSnapshot Snapshot)> _pendingBuilds = new();

    private readonly Dictionary<PhotoBoundUserInterface, (TimeSpan NextRequest, int Requests)> _awaiting = new();

    /// <summary>
    /// Snapshots of photos this player has held or opened, so opening them again doesn't wait on the server.
    /// </summary>
    /// <remarks>
    /// Keyed by net id: a freshly printed photo's snapshot can arrive before the photo entity itself does.
    /// </remarks>
    private readonly Dictionary<NetEntity, (PhotoSnapshot Snapshot, TimeSpan Received)> _known = new();

    private static readonly TimeSpan KnownSnapshotLifetime = TimeSpan.FromMinutes(30);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<PhotoSnapshotEvent>(OnSnapshotEvent);
    }

    private void OnSnapshotEvent(PhotoSnapshotEvent ev)
    {
        _known[ev.Photo] = (ev.Snapshot, _timing.RealTime);

        if (TryGetEntity(ev.Photo, out var photo)
            && _ui.TryGetOpenUi<PhotoBoundUserInterface>(photo.Value, PhotoUiKey.Key, out var ui))
        {
            ui.SetSnapshot(ev.Snapshot);
        }
    }

    public bool TryGetKnownSnapshot(EntityUid photo, [NotNullWhen(true)] out PhotoSnapshot? snapshot)
    {
        snapshot = null;
        if (!_known.TryGetValue(GetNetEntity(photo), out var known))
            return false;

        snapshot = known.Snapshot;
        return true;
    }

    public void QueueBuild(PhotoBoundUserInterface ui, PhotoSnapshot snapshot)
    {
        _pendingBuilds.Add((ui, snapshot));
    }

    /// <summary>
    /// Asks the server again until <paramref name="ui"/> has its snapshot, closes, or runs out of attempts.
    /// </summary>
    public void AwaitSnapshot(PhotoBoundUserInterface ui)
    {
        _awaiting[ui] = (_timing.RealTime + RequestRetryDelay, 1);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        RetrySnapshotRequests();

        if (_pendingBuilds.Count == 0)
            return;

        foreach (var (ui, snapshot) in _pendingBuilds)
        {
            var (map, eye, size) = Build(snapshot);
            ui.ShowScene(map, eye, size);
        }

        _pendingBuilds.Clear();
    }

    private void RetrySnapshotRequests()
    {
        var now = _timing.RealTime;
        foreach (var (photo, known) in _known.ToArray())
        {
            if (now - known.Received > KnownSnapshotLifetime)
                _known.Remove(photo);
        }

        if (_awaiting.Count == 0)
            return;

        foreach (var (ui, (nextRequest, requests)) in _awaiting.ToArray())
        {
            if (ui.Snapshot != null || ui.Disposed || requests >= MaxRequests)
            {
                _awaiting.Remove(ui);
                continue;
            }

            if (now < nextRequest)
                continue;

            ui.RequestSnapshot();
            _awaiting[ui] = (now + RequestRetryDelay, requests + 1);
        }
    }

    private (EntityUid Map, IEye Eye, Vector2i ViewportSize) Build(PhotoSnapshot snapshot)
    {
        var mapUid = _map.CreateMap(out var mapId);
        _map.SetAmbientLight(mapId, snapshot.AmbientLight);
        if (snapshot.Parallax != null)
            EnsureComp<ParallaxComponent>(mapUid).Parallax = snapshot.Parallax;

        // Built where the shot was taken, so the space background lines up with what was photographed.
        var grid = _map.CreateGridEntity(mapId);
        _transform.SetWorldPosition(grid.Owner, snapshot.WorldCenter - Center);
        var hidden = EnsureComp<PhotoHiddenTilesComponent>(grid);
        hidden.Tiles = snapshot.Hidden;
        hidden.Shadows = BuildShadows(Center + snapshot.Viewer, snapshot.Occluders);
        var tiles = new List<(Vector2i, Tile)>(snapshot.Tiles.Count);
        RoofComponent? roof = null;
        foreach (var tile in snapshot.Tiles)
        {
            if (!_tileDef.TryGetDefinition(tile.TileId, out var def))
                continue;

            tiles.Add((tile.Offset, new Tile(def.TileId, 0, tile.Variant, tile.RotationMirroring)));

            if (tile.Roof is not { } roofColor)
                continue;

            if (roof == null)
            {
                roof = EnsureComp<RoofComponent>(grid);
                roof.Color = roofColor;
            }
        }
        // Tiles go down first so anchored things like walls have something to anchor to.
        _map.SetTiles(grid, tiles);

        // Roofs are required so ambient blue starlight doesnt bleed in
        if (roof != null)
        {
            foreach (var tile in snapshot.Tiles)
            {
                if (tile.Roof != null)
                    _roof.SetRoof((grid, grid.Comp, roof), tile.Offset, true);
            }
        }

        var clones = new EntityUid?[snapshot.Entities.Count];
        for (var i = 0; i < snapshot.Entities.Count; i++)
        {
            var entity = snapshot.Entities[i];
            if (!_proto.HasIndex(entity.Prototype))
                continue;

            try
            {
                clones[i] = SpawnClone(grid, entity);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to rebuild {entity.Prototype} in photo: {e}");
            }
        }

        for (var i = 0; i < snapshot.Entities.Count; i++)
        {
            if (snapshot.Entities[i].BuckledTo is { } strapIndex
                && clones[i] is { } buckled
                && strapIndex < clones.Length
                && clones[strapIndex] is { } strap)
            {
                ApplyBuckleDrawDepth(buckled, strap);
            }
        }

        var eye = new FixedEye
        {
            Position = new MapCoordinates(snapshot.WorldCenter, mapId),
            DrawFov = false,
            Zoom = Vector2.One,
        };

        var size = (snapshot.HalfExtents * 2 + Vector2i.One) * EyeManager.PixelsPerMeter;
        return (mapUid, eye, size);
    }

    /// <summary>
    /// How far shadows reach past the wall casting them, in tiles. Anything past the frame's edge is off the photo.
    /// </summary>
    private const float ShadowLength = 64f;

    /// <summary>
    /// Shadows like the game's field of view: every wall edge facing away from <paramref name="viewer"/> is stretched
    /// away from it. Edges between two neighbouring walls are skipped, so walls don't shade each other along a wall.
    /// </summary>
    /// <returns>A triangle list in grid-local coordinates.</returns>
    private static Vector2[] BuildShadows(Vector2 viewer, List<Vector2i> occluderTiles)
    {
        var occluders = new HashSet<Vector2i>(occluderTiles);
        var triangles = new List<Vector2>();
        foreach (var tile in occluders)
        {
            var (x, y) = (tile.X, tile.Y);
            AddEdge(new Vector2i(0, -1), new Vector2(x, y), new Vector2(x + 1, y));
            AddEdge(new Vector2i(0, 1), new Vector2(x, y + 1), new Vector2(x + 1, y + 1));
            AddEdge(new Vector2i(-1, 0), new Vector2(x, y), new Vector2(x, y + 1));
            AddEdge(new Vector2i(1, 0), new Vector2(x + 1, y), new Vector2(x + 1, y + 1));

            void AddEdge(Vector2i normal, Vector2 a, Vector2 b)
            {
                if (occluders.Contains(tile + normal))
                    return;

                // Only edges facing away from the viewer start a shadow.
                if (Vector2.Dot(normal, (a + b) / 2 - viewer) <= 0)
                    return;

                var aFar = a + Away(a) * ShadowLength;
                var bFar = b + Away(b) * ShadowLength;
                triangles.AddRange([a, b, bFar, a, bFar, aFar]);
            }
        }

        return triangles.ToArray();

        Vector2 Away(Vector2 point)
        {
            var direction = point - viewer;
            return direction.LengthSquared() > 0.0001f ? Vector2.Normalize(direction) : Vector2.Zero;
        }
    }

    public void Destroy(EntityUid map)
    {
        if (!TerminatingOrDeleted(map))
            Del(map);
    }


    private void ApplyBuckleDrawDepth(EntityUid buckled, EntityUid strap)
    {
        if (!TryComp<StrapComponent>(strap, out var strapComp)
            || !strapComp.ModifyBuckleDrawDepth
            || !TryComp<SpriteComponent>(strap, out var strapSprite)
            || !TryComp<SpriteComponent>(buckled, out var buckledSprite))
        {
            return;
        }

        if (_transform.GetWorldRotation(strap).GetCardinalDir() == Direction.North)
            _sprite.SetDrawDepth((buckled, buckledSprite), strapSprite.DrawDepth - 1);
    }

    private EntityUid? SpawnClone(EntityUid grid, PhotoSnapshotEntity entity)
    {
        var coords = new EntityCoordinates(grid, Center + entity.Offset);
        var uid = entity.Humanoid is { } humanoid
            ? SpawnHumanoid(entity.Prototype, humanoid, coords)
            : Spawn(entity.Prototype, coords);
        if (uid == null)
            return null;

        // This Ensures the Drawn Prototypes get their correct Rotation otherwise they stop midway and appear crooked
        _transform.SetLocalRotationNoLerp(uid.Value, entity.Rotation);
        ApplyAppearance(uid.Value, entity.Appearance);

        if (entity.Light is { } snapshotLight)
            ApplyLight(uid.Value, snapshotLight);

        if (entity.MapText is { } mapTextState && TryComp<MapTextComponent>(uid, out var mapText))
        {
            var handleState = new ComponentHandleState(mapTextState, null);
            EntityManager.EventBus.RaiseComponentEvent(uid.Value, mapText, ref handleState);
        }

        return uid;
    }

    /// <summary>
    /// Builds a character the way the lobby preview does.
    /// </summary>
    private EntityUid? SpawnHumanoid(EntProtoId prototype, PhotoSnapshotHumanoid humanoid, EntityCoordinates coords)
    {
        if (!_proto.Resolve(humanoid.Species, out var species))
            return null;

        var doll = Spawn(species.DollPrototype, coords);

        // Dolls don't inherit BaseMob's sprite settings so we need to add draw depth and rotation
        if (_proto.Index(prototype).TryComp<SpriteComponent>(out var mobSprite, _componentFactory)
            && TryComp<SpriteComponent>(doll, out var dollSprite))
        {
            _sprite.SetDrawDepth((doll, dollSprite), mobSprite.DrawDepth);
            _sprite.SetSnapCardinals((doll, dollSprite), mobSprite.SnapCardinals);
            dollSprite.NoRotation = mobSprite.NoRotation;
        }
        _visualBody.ApplyProfiles(doll, humanoid.Profiles);
        _visualBody.ApplyMarkings(doll, humanoid.Markings);

        foreach (var (slot, worn) in humanoid.Worn)
        {
            if (SpawnItem(worn) is not { } item)
                continue;

            if (!_inventory.TryEquip(doll, item, slot, silent: true, force: true))
                Del(item);
        }

        foreach (var (hand, held) in humanoid.Held)
        {
            if (SpawnItem(held) is not { } item)
                continue;

            if (!_hands.TryPickup(doll, item, hand, checkActionBlocker: false, animate: false))
                Del(item);
        }

        return doll;
    }

    private EntityUid? SpawnItem(PhotoSnapshotItem item)
    {
        if (!_proto.HasIndex(item.Prototype))
            return null;

        var uid = Spawn(item.Prototype, MapCoordinates.Nullspace);
        ApplyAppearance(uid, item.Appearance);
        return uid;
    }

    private void ApplyAppearance(EntityUid uid, Dictionary<Enum, object>? appearance)
    {
        if (appearance == null)
            return;

        foreach (var (key, value) in appearance)
        {
            _appearance.SetData(uid, key, value);
        }

        // Character height lives in ScaleVisuals, which only applies with its component present.
        if (appearance.TryGetValue(ScaleVisuals.Scale, out var scale) && scale is Vector2 scaleVector)
            _scaleVisuals.SetSpriteScale(uid, scaleVector);
    }

    private void ApplyLight(EntityUid uid, PhotoSnapshotLight snapshotLight)
    {
        var light = _light.EnsureLight(uid);
        _light.SetEnabled(uid, snapshotLight.Enabled, light);
        _light.SetColor(uid, snapshotLight.Color, light);
        _light.SetRadius(uid, snapshotLight.Radius, light);
        _light.SetEnergy(uid, snapshotLight.Energy, light);
        _light.SetSoftness(uid, snapshotLight.Softness, light);
        _light.SetFalloff(uid, snapshotLight.Falloff, light);
        _light.SetCurveFactor(uid, snapshotLight.CurveFactor, light);
    }
}
