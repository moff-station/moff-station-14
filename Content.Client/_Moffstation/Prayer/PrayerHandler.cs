using Robust.Client.Graphics;
using Content.Client.Administration.Managers;
using Content.Shared._Moffstation.Prayers;

namespace Content.Client._Moffstation.Prayer;

public sealed class PrayerHandler : EntitySystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IClientAdminManager _adminManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<PrayerEvent>(Onprayer);
    }

    private void Onprayer(PrayerEvent args)
    {
        //check if player is an Admin && not deadming
        var admin = _adminManager.GetAdminData();
        if (admin != null)
        {

            _clyde.RequestWindowAttention();
        }
    }
}
