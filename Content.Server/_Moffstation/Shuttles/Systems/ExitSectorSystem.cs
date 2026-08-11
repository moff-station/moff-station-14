using System.Numerics;
using Content.Server._Moffstation.Shuttles.Components;
using Content.Server.Administration.Logs;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Shared._Moffstation.Shuttles.Events;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;

namespace Content.Server._Moffstation.Shuttles.Systems;

/// <summary>
/// Handles shuttle consoles leaving the sector permanently. The shuttle FTLs to a throwaway map and is deleted on
/// arrival, which despawns everything aboard and ghosts every player via <c>MindContainerComponent.GhostOnShutdown</c>.
/// </summary>
public sealed partial class ExitSectorSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ShuttleSystem _shuttle = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<ShuttleConsoleComponent>(ShuttleConsoleUiKey.Key,
            subs => subs.Event<ShuttleConsoleExitSectorMessage>(OnExitSector));

        SubscribeLocalEvent<ExitingSectorComponent, FTLCompletedEvent>(OnExitCompleted);
    }

    private void OnExitSector(Entity<ShuttleConsoleComponent> ent, ref ShuttleConsoleExitSectorMessage args)
    {
        if (!ent.Comp.CanExitSector)
            return;

        if (Transform(ent).GridUid is not { } shuttleUid ||
            !TryComp(shuttleUid, out ShuttleComponent? shuttleComp) ||
            !shuttleComp.Enabled ||
            HasComp<ExitingSectorComponent>(shuttleUid))
            return;

        if (!_shuttle.CanFTL(shuttleUid, out var reason))
        {
            _popup.PopupCursor(reason, args.Actor);
            return;
        }

        var mapUid = _mapSystem.CreateMap(out _);
        _metaData.SetEntityName(mapUid, "Exited Sector");
        EnsureComp<ExitingSectorComponent>(shuttleUid).ExitMap = mapUid;

        _adminLogger.Add(LogType.Action,
            LogImpact.Extreme,
            $"{ToPrettyString(args.Actor):player} made {ToPrettyString(shuttleUid):shuttle} exit the sector via {ToPrettyString(ent):console}");

        _shuttle.FTLToCoordinates(shuttleUid, shuttleComp, new EntityCoordinates(mapUid, Vector2.Zero), Angle.Zero);
    }

    private void OnExitCompleted(Entity<ExitingSectorComponent> ent, ref FTLCompletedEvent args)
    {
        // Deleting the map takes the shuttle and everyone on it with it. QueueDel because this fires mid-enumeration.
        if (ent.Comp.ExitMap is { } map && !TerminatingOrDeleted(map))
            QueueDel(map);
        else
            QueueDel(ent.Owner);
    }
}
