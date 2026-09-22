using Content.Server._Moffstation.PDA.Ringer;
using Content.Shared._Moffstation.CCVar;
using Content.Shared.PDA;
using Content.Shared.PDA.Ringer;
using Content.Shared.Popups;
using Content.Shared.Roles.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.PDA.Ringer;

public sealed partial class RingerSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedPdaSystem _pda = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    [Dependency] private EntityQuery<LockableUplinkBlockedMapComponent> _mapBlockedQuery;

    /// <summary>
    /// Returns if the uplink is allowed to be opened.
    /// </summary>
    /// <param name="uplink">Entity containing the uplink.</param>
    /// <returns></returns>
    private bool UplinkAllowed(EntityUid uplink)
    {
        if (!_cfg.GetCVar(MoffCCVars.GameBlockUplinks))
            return true;

        var uplinkMapId = _transform.GetMap(uplink);
        return !_mapBlockedQuery.HasComp(uplinkMapId);
    }

    /// <summary>
    /// Checks that a map allows for uplinks to be opened before opening an uplink.
    /// </summary>
    /// <param name="ent">PDA that holds the uplink</param>
    /// <param name="args"></param>
    [SubscribeLocalEvent]
    private void BeforeOpenUplink(Entity<RingerUplinkComponent> ent, ref BeforeUplinkOpenEvent args)
    {
        if (UplinkAllowed(ent))
            return;

        args.Canceled = true;

        _popupSystem.PopupEntity(Loc.GetString("uplink-no-connection"),
            ent,
            args.AttemptingClient,
            PopupType.LargeCaution);
    }

    [SubscribeLocalEvent]
    private void OnMapUidChanged(Entity<RingerUplinkComponent> ent, ref MapUidChangedEvent args)
    {
        UpdateLockStatus(ent);
    }

    [SubscribeLocalEvent]
    private void OnMapLockStatusUpdated(ref MapLockStatusUpdated args)
    {
        foreach (var ent in EntityQueryEnumerator<RingerUplinkComponent>())
        {
            UpdateLockStatus(ent);
        }
    }

    private void UpdateLockStatus(Entity<RingerUplinkComponent> ent)
    {
        if (!ent.Comp.Unlocked)
            return;

        if (!UplinkAllowed(ent.Owner))
            return;

        LockUplink(ent!);
        _pda.UpdatePdaUi(ent.Owner);

        if (!_container.TryGetContainingContainer(ent.Owner, out var container) || container is not { } baseContainer)
            return;

        var wearer = baseContainer.Owner;

        _popupSystem.PopupEntity(
            Loc.GetString("uplink-lose-connection"),
            ent.Owner,
            wearer,
            PopupType.LargeCaution);
    }
}

[ByRefEvent]
public record struct BeforeUplinkOpenEvent(EntityUid AttemptingClient, bool Canceled = false);

[ByRefEvent]
public record struct MapLockStatusUpdated();
