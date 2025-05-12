using Content.Server.Chat.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Radio;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._CD.Roles;

public sealed partial class NotifyDepartmentSpecial : JobSpecial
{
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    public override void AfterEquip(EntityUid mob)
    {
        // Notify people on all stations.
        _chatSystem.DispatchStationAnnouncement(mob, Loc.GetString("prisoner-arrivals-notice"), colorOverride: Color.Orange);
    }
}
