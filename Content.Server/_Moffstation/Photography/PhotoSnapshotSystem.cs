using System.Numerics;
using Content.Server.MapText;
using Content.Shared._Moffstation.Photography;
using Content.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.MapText;
using Content.Shared.Parallax;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Photography;

/// <summary>
/// Prints photographs from <see cref="SnapshotCameraComponent"/> cameras, records what the photographer could see
/// and ensures no arbitary data is send
/// </summary>
public sealed partial class PhotoSnapshotSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private SharedStealthSystem _stealth = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedRoofSystem _roof = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SnapshotCameraSystem _camera = default!;
    [Dependency] private ITileDefinitionManager _tileDef = default!;

    private const int MaxEntities = 256;

    private const int MaxOccluders = 512;

    /// <summary>
    /// How far the photographer can see when deciding which tiles made it into the shot.
    /// </summary>
    private const float SightRange = 20f;

    /// <summary>
    /// Stealthed entities fainter than this are left out of the photo.
    /// </summary>
    private const float MinStealthVisibility = 0.5f;

    private static readonly Vector2i[] Neighbours = [new(1, 0), new(-1, 0), new(0, 1), new(0, -1)];

    /// <summary>
    /// Points within a tile checked for line of sight: the middle, edge midpoints and corners. Kept inside the tile so
    /// a ray grazing a wall's corner can't count the tile behind it as seen.
    /// </summary>
    private static readonly Vector2[] TileSamples =
    [
        new(0.5f, 0.5f),
        new(0.5f, 0.15f), new(0.5f, 0.85f), new(0.15f, 0.5f), new(0.85f, 0.5f),
        new(0.15f, 0.15f), new(0.85f, 0.15f), new(0.15f, 0.85f), new(0.85f, 0.85f),
    ];

    private readonly HashSet<EntityUid> _found = new();

    private readonly Dictionary<EntityUid, int> _captured = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SnapshotCameraComponent, SnapshotCameraShotEvent>(OnShot);
        SubscribeLocalEvent<PhotoSnapshotComponent, GotEquippedHandEvent>(OnPickedUp);
        Subs.BuiEvents<PhotoSnapshotComponent>(PhotoUiKey.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnUiOpened);
                subs.Event<PhotoSnapshotRequestMessage>(OnSnapshotRequested);
            });
    }

    private void OnShot(Entity<SnapshotCameraComponent> ent, ref SnapshotCameraShotEvent args)
    {
        if (Capture(args.User, args.Center, ent.Comp.HalfExtents) is not { } snapshot)
            return;

        var photo = Spawn(ent.Comp.Photograph, Transform(ent).Coordinates);
        EnsureComp<PhotoSnapshotComponent>(photo).Snapshot = snapshot;
        _hands.PickupOrDrop(args.User, photo, dropNear: true);
    }

    /// <summary>
    /// Sends the snapshot to the one viewer who asked for it.
    /// </summary>
    /// <remarks>
    /// This Doesnt Recheck the UI state once it is opened to block Forged open requests
    /// </remarks>
    /// <summary>
    /// Almost Instantly gives the Entity holding it a snapshot so tehy dont have to wait for server
    /// </summary>
    private void OnPickedUp(Entity<PhotoSnapshotComponent> ent, ref GotEquippedHandEvent args)
    {
        if (ent.Comp.Snapshot is not { } snapshot || !TryComp<ActorComponent>(args.User, out var actor))
            return;

        RaiseNetworkEvent(new PhotoSnapshotEvent(GetNetEntity(ent), snapshot), actor.PlayerSession);
    }

    private void OnSnapshotRequested(Entity<PhotoSnapshotComponent> ent, ref PhotoSnapshotRequestMessage args)
    {
        SendSnapshot(ent, args.Actor);
    }

    /// <summary>
    /// Sends the snapshot right away so the photo doesn't wait on the client's request
    /// the request stays as a fallback for when this arrives before the client has the UI open.
    /// </summary>
    private void OnUiOpened(Entity<PhotoSnapshotComponent> ent, ref BoundUIOpenedEvent args)
    {
        SendSnapshot(ent, args.Actor);
    }

    private void SendSnapshot(Entity<PhotoSnapshotComponent> ent, EntityUid viewer)
    {
        if (ent.Comp.Snapshot is not { } snapshot)
            return;

        if (!_ui.IsUiOpen(ent.Owner, PhotoUiKey.Key, viewer) || !_interaction.InRangeUnobstructed(viewer, ent.Owner))
        {
            _ui.CloseUi(ent.Owner, PhotoUiKey.Key, viewer);
            return;
        }

        if (!TryComp<ActorComponent>(viewer, out var actor))
            return;

        RaiseNetworkEvent(new PhotoSnapshotEvent(GetNetEntity(ent), snapshot), actor.PlayerSession);
    }

    /// <summary>
    /// Records Lighting and such around the Photographer
    /// </summary>
    public PhotoSnapshot? Capture(EntityUid viewer, MapCoordinates shotCenter, Vector2i halfExtents)
    {
        var viewerCoords = _transform.GetMapCoordinates(viewer);
        var mapId = shotCenter.MapId;
        if (mapId == MapId.Nullspace || viewerCoords.MapId != mapId)
            return null;

        var visMask = TryComp<EyeComponent>(viewer, out var eye) ? eye.VisibilityMask : EyeComponent.DefaultVisibilityMask;

        var snapshot = new PhotoSnapshot { HalfExtents = halfExtents };

        // Same frame the viewfinder showed, so the photo matches what was aimed at.
        var frame = _camera.GetFrame(shotCenter, halfExtents);
        Entity<MapGridComponent>? grid = frame.Grid is { } gridUid && TryComp<MapGridComponent>(gridUid, out var gridComp)
            ? (gridUid, gridComp)
            : null;
        var frameRotation = frame.Rotation;
        var frameToWorld = frame.FrameToWorld;
        var worldToFrame = frame.WorldToFrame;
        var centerTile = frame.CenterTile;
        var center = centerTile + new Vector2(0.5f);

        // The photo only has the tiles that made it into the shot, and anything without a tile gets the map's ambient light.
        snapshot.AmbientLight = grid is { } roofGrid && GetRoof(roofGrid, centerTile) is { } indoorRoof
            ? indoorRoof
            : TryComp<MapLightComponent>(_map.GetMapOrInvalid(mapId), out var mapLight)
                ? mapLight.AmbientLightColor
                : Color.Black;

        // A tile is in the photo if the photographer can see any part of it, not just its middle, so partly visible
        // tiles next to walls aren't blacked out.
        var visible = new HashSet<Vector2i>();
        for (var x = -halfExtents.X; x <= halfExtents.X; x++)
        {
            for (var y = -halfExtents.Y; y <= halfExtents.Y; y++)
            {
                var offset = new Vector2i(x, y);
                var index = centerTile + offset;
                if (CanSeeAnyOfTile(viewerCoords, index, frameToWorld, worldToFrame, mapId))
                    visible.Add(offset);
            }
        }

        // Walls seen from the side add their own tile, so they have floor to anchor to and a roof over them.
        var tilesInShot = new HashSet<Vector2i>(visible);

        _found.Clear();
        _captured.Clear();
        var frameBox = new Box2(centerTile - halfExtents, centerTile + halfExtents + Vector2i.One);
        if (grid is { } lookupGrid)
            _lookup.GetLocalEntitiesIntersecting(lookupGrid, frameBox, _found, LookupFlags.Uncontained);
        else
            _found.UnionWith(_lookup.GetEntitiesIntersecting(mapId, frameBox, LookupFlags.Uncontained));

        foreach (var uid in _found)
        {
            if (snapshot.Entities.Count >= MaxEntities)
                break;

            if (!ShouldInclude(uid, visMask, grid?.Owner) || MetaData(uid).EntityPrototype is not { } proto)
                continue;

            var xform = Transform(uid);
            var local = Vector2.Transform(_transform.GetWorldPosition(xform), worldToFrame);
            var tileOffset = TileOf(local) - centerTile;
            if (Math.Abs(tileOffset.X) > halfExtents.X || Math.Abs(tileOffset.Y) > halfExtents.Y)
                continue;

            if (!visible.Contains(tileOffset))
            {
                if (!HasComp<OccluderComponent>(uid) || !HasVisibleNeighbour(tileOffset, visible))
                    continue;

                tilesInShot.Add(tileOffset);
            }

            _captured[uid] = snapshot.Entities.Count;
            snapshot.Entities.Add(new PhotoSnapshotEntity
            {
                Prototype = proto.ID,
                Offset = local - center,
                Rotation = _transform.GetWorldRotation(xform) - frameRotation,
                Appearance = GetAppearance(uid),
                Light = GetLight(uid),
                MapText = GetMapText(uid),
                Humanoid = GetHumanoid(uid, visMask),
            });
        }

        foreach (var (uid, index) in _captured)
        {
            if (TryComp<BuckleComponent>(uid, out var buckle)
                && buckle.BuckledTo is { } strap
                && _captured.TryGetValue(strap, out var strapIndex))
            {
                snapshot.Entities[index].BuckledTo = strapIndex;
            }
        }

        if (grid is { } g)
        {
            foreach (var offset in tilesInShot)
            {
                var index = centerTile + offset;
                if (!_map.TryGetTileRef(g, g.Comp, index, out var tileRef)
                    || tileRef.Tile.IsEmpty
                    || !_tileDef.TryGetDefinition(tileRef.Tile.TypeId, out var tileDef))
                {
                    continue;
                }

                snapshot.Tiles.Add(new PhotoSnapshotTile(offset,
                    tileDef.ID,
                    tileRef.Tile.Variant,
                    tileRef.Tile.RotationMirroring,
                    GetRoof(g, index)));
            }
        }

        for (var x = -halfExtents.X; x <= halfExtents.X; x++)
        {
            for (var y = -halfExtents.Y; y <= halfExtents.Y; y++)
            {
                var offset = new Vector2i(x, y);
                if (!tilesInShot.Contains(offset))
                    snapshot.Hidden.Add(offset);
            }
        }

        snapshot.WorldCenter = Vector2.Transform(center, frameToWorld);
        snapshot.Parallax = TryComp<ParallaxComponent>(_map.GetMapOrInvalid(mapId), out var parallax)
            ? parallax.Parallax
            : null;

        var viewerLocal = Vector2.Transform(viewerCoords.Position, worldToFrame);
        snapshot.Viewer = viewerLocal - center;
        if (grid is { } occluderGrid)
            AddSeenOccluders(snapshot, occluderGrid, viewerCoords, viewerLocal, centerTile, frameToWorld, worldToFrame);

        return snapshot;
    }

    /// <summary>
    /// Records the sight-blocking tiles between the photographer and the frame, including ones just outside it that
    /// cast shadows into it, so the viewer can draw shadows like the game's field of view.
    /// </summary>
    private void AddSeenOccluders(PhotoSnapshot snapshot,
        Entity<MapGridComponent> grid,
        MapCoordinates viewer,
        Vector2 viewerLocal,
        Vector2i centerTile,
        Matrix3x2 frameToWorld,
        Matrix3x2 worldToFrame)
    {
        var frameBox = new Box2(centerTile - snapshot.HalfExtents, centerTile + snapshot.HalfExtents + Vector2i.One);
        var area = frameBox.ExtendToContain(viewerLocal).Enlarged(1f);

        _found.Clear();
        _lookup.GetLocalEntitiesIntersecting(grid, area, _found, LookupFlags.Static);

        var checkedTiles = new HashSet<Vector2i>();
        foreach (var uid in _found)
        {
            if (snapshot.Occluders.Count >= MaxOccluders)
                break;

            if (!TryComp<OccluderComponent>(uid, out var occluder) || !occluder.Enabled)
                continue;

            var tile = TileOf(uid, worldToFrame);
            if (!checkedTiles.Add(tile))
                continue;

            if (CanSeeAnyOfTile(viewer, tile, frameToWorld, worldToFrame, viewer.MapId))
                snapshot.Occluders.Add(tile - centerTile);
        }
    }

    private bool ShouldInclude(EntityUid uid, int visMask, EntityUid? grid)
    {
        if (uid == grid || HasComp<MapComponent>(uid) || HasComp<MapGridComponent>(uid))
            return false;

        if (!IsVisibleTo(uid, visMask))
            return false;

        return !_container.IsEntityOrParentInContainer(uid);
    }

    /// <summary>
    /// PVS layer rules to not accidentily capture ghosts and such
    /// </summary>
    private bool IsVisibleTo(EntityUid uid, int visMask)
    {
        var meta = MetaData(uid);
        if ((visMask & meta.VisibilityMask) != meta.VisibilityMask)
            return false;

        return !TryComp<StealthComponent>(uid, out var stealth)
            || !stealth.Enabled
            || _stealth.GetVisibility(uid, stealth) >= MinStealthVisibility;
    }

    private Color? GetRoof(Entity<MapGridComponent> grid, Vector2i index)
    {
        if (TryComp<RoofComponent>(grid, out var roof))
            return _roof.GetColor((grid, grid.Comp, roof), index);

        return TryComp<ImplicitRoofComponent>(grid, out var implicitRoof) ? implicitRoof.Color : null;
    }

    /// <summary>
    /// Records a character´s apperance
    /// </summary>
    private PhotoSnapshotHumanoid? GetHumanoid(EntityUid uid, int visMask)
    {
        if (!TryComp<HumanoidProfileComponent>(uid, out var profile)
            || !_visualBody.TryGatherMarkingsData(uid, null, out var profiles, out _, out var markings))
        {
            return null;
        }

        var humanoid = new PhotoSnapshotHumanoid
        {
            Species = profile.Species,
            Profiles = profiles,
            Markings = markings,
        };

        if (_inventory.TryGetSlots(uid, out var slots))
        {
            foreach (var slot in slots)
            {
                // Pockets and the like don't show on the sprite, so their contents stay private even if someone
                // tried to read image data elicitly.
                if (slot.StripHidden || !_inventory.TryGetSlotEntity(uid, slot.Name, out var worn))
                    continue;

                if (GetItem(worn.Value, visMask) is { } item)
                    humanoid.Worn[slot.Name] = item;
            }
        }

        foreach (var hand in _hands.EnumerateHands(uid))
        {
            if (!_hands.TryGetHeldItem(uid, hand, out var held)
                || HasComp<VirtualItemComponent>(held)
                || GetItem(held.Value, visMask) is not { } item)
            {
                continue;
            }

            humanoid.Held[hand] = item;
        }

        return humanoid;
    }

    private PhotoSnapshotItem? GetItem(EntityUid uid, int visMask)
    {
        if (!IsVisibleTo(uid, visMask) || MetaData(uid).EntityPrototype is not { } proto)
            return null;

        return new PhotoSnapshotItem(proto.ID, GetAppearance(uid));
    }

    /// <summary>
    /// Copies the appearance data the server already networks
    /// </summary>
    private Dictionary<Enum, object>? GetAppearance(EntityUid uid)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance) || !appearance.NetSyncEnabled)
            return null;

        if (EntityManager.GetComponentState(EntityManager.EventBus, appearance, null, GameTick.Zero)
                is not AppearanceComponentState { Data.Count: > 0 } state)
        {
            return null;
        }

        return new Dictionary<Enum, object>(state.Data);
    }

    private MapTextComponentState? GetMapText(EntityUid uid)
    {
        if (!TryComp<MapTextComponent>(uid, out var mapText))
            return null;

        return EntityManager.GetComponentState(EntityManager.EventBus, mapText, null, GameTick.Zero) as MapTextComponentState;
    }

    private PhotoSnapshotLight? GetLight(EntityUid uid)
    {
        if (!_light.TryGetLight(uid, out var light))
            return null;

        return new PhotoSnapshotLight(light.Enabled,
            light.Color,
            light.Radius,
            light.Energy,
            light.Softness,
            light.Falloff,
            light.CurveFactor);
    }

    private bool CanSeeAnyOfTile(MapCoordinates viewer, Vector2i index, Matrix3x2 frameToWorld, Matrix3x2 worldToFrame, MapId mapId)
    {
        foreach (var sample in TileSamples)
        {
            var point = new MapCoordinates(Vector2.Transform(index + sample, frameToWorld), mapId);

            // Whatever stands on the tile itself shouldn't hide it.
            if (_examine.InRangeUnOccluded(viewer,
                    point,
                    SightRange,
                    (Index: index, WorldToFrame: worldToFrame),
                    (uid, state) => TileOf(uid, state.WorldToFrame) == state.Index))
            {
                return true;
            }
        }

        return false;
    }

    private Vector2i TileOf(EntityUid uid, Matrix3x2 worldToFrame)
    {
        return TileOf(Vector2.Transform(_transform.GetWorldPosition(uid), worldToFrame));
    }

    private static Vector2i TileOf(Vector2 local)
    {
        return new Vector2i((int) MathF.Floor(local.X), (int) MathF.Floor(local.Y));
    }

    private static bool HasVisibleNeighbour(Vector2i tile, HashSet<Vector2i> visible)
    {
        foreach (var dir in Neighbours)
        {
            if (visible.Contains(tile + dir))
                return true;
        }

        return false;
    }
}
