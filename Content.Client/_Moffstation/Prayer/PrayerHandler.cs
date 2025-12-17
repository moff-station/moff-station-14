using Robust.Client.Graphics;
using Content.Client.Administration.Managers;
using Content.Shared._Moffstation.Prayers;

namespace Content.Client._Moffstation.Prayer;

public sealed class PrayerHandler : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<PrayerEvent>(Onprayer);
    }
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IClientAdminManager _adminManager = default!;

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
