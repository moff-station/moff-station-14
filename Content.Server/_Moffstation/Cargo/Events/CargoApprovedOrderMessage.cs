using Content.Shared.Cargo.Prototypes;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Cargo.Events;

public sealed class CargoApprovedOrderMessage(
    int orderId,
    ProtoId<CargoAccountPrototype> account,
    bool shouldAnnounceFulfillment,
    EntityUid approver,
    EntityUid approvedOnDevice,
    ProtoId<RadioChannelPrototype> announcementChannel
)
{
    public readonly int OrderId = orderId;
    public readonly ProtoId<CargoAccountPrototype> Account = account;
    public readonly bool ShouldAnnounceFulfillment = shouldAnnounceFulfillment;
    public readonly EntityUid Approver = approver;
    public readonly EntityUid ApprovedOnDevice = approvedOnDevice;
    public readonly ProtoId<RadioChannelPrototype> AnnouncementChannel = announcementChannel;

    public string? DenialReason = null;
}
