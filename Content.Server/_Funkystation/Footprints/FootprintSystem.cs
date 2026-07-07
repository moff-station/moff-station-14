using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._Funkystation.Footprints;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.Inventory;
using Content.Shared.Standing;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Funkystation.Footprints;

public sealed partial class FootprintSystem : EntitySystem
{
    [Dependency] private TransformSystem _transform = null!;
    [Dependency] private SharedMapSystem _map = null!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = null!;
    [Dependency] private SharedPuddleSystem _puddle = null!;
    [Dependency] private IPrototypeManager _prototypeManager = null!;
    [Dependency] private IRobustRandom _random = null!;
    [Dependency] private InventorySystem _inventory = null!;

    private static readonly FixedPoint2 MaxVolumePerTile = 50;
    private static readonly EntProtoId FootprintEntityId = "Footprint";
    private const string PrintSolutionName = "print";
    private const string PuddleTargetSolution = "puddle";

    private static readonly string[] DragStates =
    [
        "dragging-1",
        "dragging-2",
        "dragging-3",
        "dragging-4",
        "dragging-5"
    ];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FootprintComponent, FootprintCleanEvent>(OnFootprintCleaned);
        SubscribeLocalEvent<FootprintOwnerComponent, MoveEvent>(OnEntityMoved);
        SubscribeLocalEvent<PuddleComponent, MapInitEvent>(OnPuddleInit);

        // Listen for chemical changes (like Space Cleaner)
        SubscribeLocalEvent<FootprintComponent, SolutionChangedEvent>(OnSolutionChanged);
    }

    private void OnSolutionChanged(EntityUid uid, FootprintComponent component, ref SolutionChangedEvent args)
    {
        UpdatePrintColors(uid, component);
    }

    private void UpdatePrintColors(EntityUid uid, FootprintComponent component)
    {
        if (!TryGetPrintSolution(uid, out var solution))
            return;

        var newBaseColor = solution.Comp.Solution.GetColor(_prototypeManager);

        for (var i = 0; i < component.Prints.Count; i++)
        {
            var print = component.Prints[i];
            // Update the RGB while preserving the original alpha (transparency) of that specific step
            var updatedColor = newBaseColor.WithAlpha(print.Color.A);
            component.Prints[i] = print with { Color = updatedColor };
        }

        Dirty(uid, component);
        RaiseNetworkEvent(new FootprintStateEvent(GetNetEntity(uid)), Filter.Pvs(uid));
    }

    private void OnFootprintCleaned(EntityUid uid, FootprintComponent component, ref FootprintCleanEvent args)
    {
        TurnIntoPuddle(uid);
    }

    private void OnEntityMoved(EntityUid uid, FootprintOwnerComponent component, ref MoveEvent args)
    {
        if (HasComp<NoFootprintsComponent>(uid))
            return;

        if (_inventory.TryGetSlotEntity(uid, "shoes", out var shoes) && HasComp<NoFootprintsComponent>(shoes))
            return;

        if (!args.OldPosition.IsValid(EntityManager) || !args.NewPosition.IsValid(EntityManager))
            return;

        var prevPos = _transform.ToMapCoordinates(args.OldPosition).Position;
        var currentPos = _transform.ToMapCoordinates(args.NewPosition).Position;

        component.DistanceWalked += Vector2.Distance(currentPos, prevPos);

        var isStanding = !TryComp<StandingStateComponent>(uid, out var standing) || standing.Standing;
        var requiredDistance = isStanding ? component.FootstepDistance : component.DragDistance;

        if (component.DistanceWalked < requiredDistance)
            return;

        component.DistanceWalked -= requiredDistance;

        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var oldLocal = _map.WorldToLocal(gridUid, grid, prevPos);
        var newLocal = _map.WorldToLocal(gridUid, grid, currentPos);
        var moveVector = newLocal - oldLocal;

        if (moveVector.LengthSquared() < 0.0001f)
            return;

        var walkAngle = moveVector.ToAngle();
        var rotation = walkAngle + Angle.FromDegrees(90);

        var stepOffset = isStanding ? component.AlternateStepOffset : 0f;
        component.AlternateStepOffset = -component.AlternateStepOffset;

        var rightVector = new Angle(walkAngle.Theta - Math.PI / 2).ToVec();
        var offsetPos = newLocal + rightVector * stepOffset;

        var coords = new EntityCoordinates(gridUid, offsetPos);
        var tileIndices = _map.CoordinatesToTile(gridUid, grid, coords);

        if (ProcessPuddleStepping(uid, component, gridUid, grid, tileIndices, isStanding))
            return;

        CreateFootprint(uid, component, gridUid, grid, tileIndices, coords, rotation, isStanding);
    }

    private bool ProcessPuddleStepping(EntityUid uid, FootprintOwnerComponent component, EntityUid gridUid, MapGridComponent grid, Vector2i tile, bool isStanding)
    {
        // Footprints carry a PuddleComponent too (for their own colour/volume), so explicitly skip them here -
        // otherwise players would "step in" their own trail and wash their feet on it.
        if (!TryGetAnchoredEntity<PuddleComponent>(gridUid, grid, tile, out var puddleUid, out _, HasComp<FootprintComponent>))
            return false;

        if (!_solutionContainer.TryGetSolution(puddleUid, PuddleTargetSolution, out var puddleSolution, out _))
            return false;

        if (!TryGetPrintSolution(uid, out var ownerSolution))
            return false;

        var maxStorage = isStanding ? component.MaxFootVolume : component.MaxBodyVolume;
        var spaceLeft = FixedPoint2.Max(0, maxStorage - ownerSolution.Comp.Solution.Volume);

        _solutionContainer.TryTransferSolution(ownerSolution, puddleSolution.Value.Comp.Solution, spaceLeft);
        _solutionContainer.UpdateChemicals(ownerSolution, false);
        _solutionContainer.UpdateChemicals(puddleSolution.Value, false);
        return true;
    }

    private void CreateFootprint(EntityUid uid, FootprintOwnerComponent component, EntityUid gridUid, MapGridComponent grid, Vector2i tile, EntityCoordinates coords, Angle rotation, bool isStanding)
    {
        if (!TryGetPrintSolution(uid, out var ownerSolution))
            return;

        var transferAmount = CalculateTransferVolume(component, ownerSolution, isStanding);
        if (transferAmount < component.MinPrintVolume)
            return;

        if (!TryGetAnchoredEntity<FootprintComponent>(gridUid, grid, tile, out var printUid, out var printComp))
        {
            printUid = Spawn(FootprintEntityId, coords);
            printComp = Comp<FootprintComponent>(printUid);
        }

        if (!TryGetPrintSolution(printUid, out var printSolution))
            return;

        var maxVol = isStanding ? component.MaxFootprintVolume : component.MaxBodyprintVolume;
        var alpha = (float)transferAmount / maxVol / 2f;
        var color = ownerSolution.Comp.Solution.GetColor(_prototypeManager).WithAlpha(alpha);

        _solutionContainer.TryTransferSolution(printSolution, ownerSolution.Comp.Solution, transferAmount);
        _solutionContainer.UpdateChemicals(printSolution, false);
        _solutionContainer.UpdateChemicals(ownerSolution, false);

        if (printSolution.Comp.Solution.Volume >= MaxVolumePerTile)
        {
            var solClone = printSolution.Comp.Solution.Clone();
            QueueDel(printUid);
            _puddle.TrySpillAt(coords, solClone, out _, false);
            return;
        }

        var localPosition = coords.Position;
        var normX = (localPosition.X / grid.TileSize) - MathF.Floor(localPosition.X / grid.TileSize) - (grid.TileSize / 2f);
        var normY = (localPosition.Y / grid.TileSize) - MathF.Floor(localPosition.Y / grid.TileSize) - (grid.TileSize / 2f);

        var state = isStanding ? "foot" : _random.Pick(DragStates);

        printComp.Prints.Add(new FootprintData(new Vector2(normX, normY), rotation, color, state));
        Dirty(printUid, printComp);

        RaiseNetworkEvent(new FootprintStateEvent(GetNetEntity(printUid)), Filter.Pvs(printUid));
    }

    private void OnPuddleInit(EntityUid uid, PuddleComponent component, ref MapInitEvent args)
    {
        if (HasComp<FootprintComponent>(uid))
            return;

        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var tile = _map.CoordinatesToTile(gridUid, grid, xform.Coordinates);
        if (TryGetAnchoredEntity<FootprintComponent>(gridUid, grid, tile, out var printUid, out _))
        {
            TurnIntoPuddle(printUid, xform.Coordinates);
        }
    }

    private void TurnIntoPuddle(EntityUid printUid, EntityCoordinates? coords = null)
    {
        var targetCoords = coords ?? Transform(printUid).Coordinates;

        if (TryGetPrintSolution(printUid, out var printSolution))
        {
            var clone = printSolution.Comp.Solution.Clone();
            QueueDel(printUid);
            _puddle.TrySpillAt(targetCoords, clone, out _, false);
        }
        else
        {
            QueueDel(printUid);
        }
    }

    private bool TryGetPrintSolution(EntityUid uid, out Entity<SolutionComponent> solution)
    {
        if (_solutionContainer.TryGetSolution(uid, PrintSolutionName, out var sol, out _))
        {
            solution = sol.Value;
            return true;
        }

        solution = default;
        return false;
    }

    private FixedPoint2 CalculateTransferVolume(FootprintOwnerComponent component, Entity<SolutionComponent> sol, bool isStanding)
    {
        var vol = sol.Comp.Solution.Volume;
        var maxVolume = isStanding ? component.MaxFootVolume : component.MaxBodyVolume;
        var minPrint = isStanding ? component.MinPrintVolume : component.MinBodyPrintVolume;
        var maxPrint = isStanding ? component.MaxFootprintVolume : component.MaxBodyprintVolume;

        var fraction = vol / maxVolume;
        var spread = maxPrint - minPrint;
        return FixedPoint2.Min(vol, (spread * fraction) + minPrint);
    }

    private bool TryGetAnchoredEntity<T>(
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i tile,
        out EntityUid entityUid,
        [NotNullWhen(true)] out T? component,
        Func<EntityUid, bool>? skip = null) where T : IComponent
    {
        var enumerator = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);
        while (enumerator.MoveNext(out var uid))
        {
            if (skip != null && skip(uid.Value))
                continue;

            if (TryComp(uid, out component))
            {
                entityUid = uid.Value;
                return true;
            }
        }

        entityUid = EntityUid.Invalid;
        component = default;
        return false;
    }
}