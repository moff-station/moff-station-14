using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Shared._Moffstation.Chitter;

/// Handles Chitter Server behavior specific to the <see cref="ChitterMpnServerComponent"/>.
public sealed partial class ChitterMpnServerSystem : EntitySystem
{
    [Dependency] private ChitterClientSystem _client = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void InteractUsing(Entity<ChitterMpnServerComponent> mpn, ref InteractUsingEvent args)
    {
        if (_client.GetClientOrNull(args.Used) is not { } client)
            return;

        if (_client.Connect(client, mpn.Owner, enforceDiscoverability: false))
        {
            _popup.PopupEntity(Loc.GetString(mpn.Comp.ConnectedPopup, ("server", mpn)), args.Used, args.User);
        }
    }
}
