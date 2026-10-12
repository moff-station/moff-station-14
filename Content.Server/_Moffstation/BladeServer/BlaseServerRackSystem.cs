using Content.Server._Moffstation.Power.EntitySystems;
using Content.Shared._Moffstation.BladeServer;
using Content.Shared.Item;

namespace Content.Server._Moffstation.BladeServer;

/// <summary>
/// This system extends <see cref="SharedBladeServerSystem"/> with sever-only power interactions.
/// </summary>
public sealed partial class BladeServerSystem : SharedBladeServerSystem
{
    [Dependency] private InnerCableSystem _innerCable = default!;

    protected override void SetSlotPower(Entity<BladeServerRackComponent> entity, BladeSlot slot, bool powered)
    {
        base.SetSlotPower(entity, slot, powered);

        if (slot.Slot.ContainerSlot?.ID is not { } containerId)
            return;

        _innerCable.SetInnerProviderContainerConnectable(entity.Owner, containerId, powered);
    }

    /// This prevents blade servers from being picked up while inside a rack.
    [SubscribeLocalEvent]
    private void OnGettingPickedUpAttempt(
        Entity<BladeServerComponent> entity,
        ref GettingPickedUpAttemptEvent args
    )
    {
        if (GetContainingRackAndSlot(entity.AsNullable())?.slot is { Ejecting: false })
        {
            args.Cancel();
        }
    }
}
