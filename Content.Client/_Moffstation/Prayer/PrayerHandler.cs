using Robust.Client.Graphics;
using Content.Client.Administration.Managers;
using Content.Shared._Moffstation.Prayers;

namespace Content.Client._Moffstation.Prayer;

public sealed class AdminPrayerSystem : EntitySystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IClientAdminManager _adminManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<PrayerEvent>(OnPrayer);
    }

    private void OnPrayer(PrayerEvent args)
    {
        //check if player is an Admin && not deadming
        if (_adminManager.GetAdminData() == null)
            return;

        _clyde.RequestWindowAttention();
    }
}
