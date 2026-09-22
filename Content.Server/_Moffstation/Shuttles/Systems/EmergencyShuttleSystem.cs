using Content.Server._Moffstation.PDA.Ringer;
using Content.Server.PDA.Ringer;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;

namespace Content.Server.Shuttles.Systems;

public sealed partial class EmergencyShuttleSystem
{
    /// <summary>
    /// This is ran when the emergency shuttle docks with the station (and subsequently Central Command)
    /// </summary>
    [SubscribeLocalEvent]
    private void OnDock(Entity<EmergencyShuttleComponent> ent, ref DockEvent args)
    {
        var map = _transformSystem.GetMap(ent.Owner);

        if (map is not { } mapUid)
            return;

        EnsureComp<LockableUplinkBlockedMapComponent>(mapUid);
        var ev = new MapLockStatusUpdated();
        RaiseLocalEvent(ref ev);
    }
}
