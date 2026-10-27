using Content.Server.Power.EntitySystems;
using Content.Shared._Moffstation.Power.Components;
using Content.Shared.Power;

namespace Content.Server._Moffstation.Power;

/// <inheritdoc/>
public sealed partial class ServerPowerConsumerVisualsSystem : PowerConsumerVisualsSystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [SubscribeLocalEvent]
    private void OnPowerConsumerReceivedChanged(
        Entity<PowerConsumerVisualsComponent> entity,
        ref PowerConsumerReceivedChanged args
    )
    {
        entity.Comp.Powered = args.ReceivedPower >= args.DrawRate;
        Dirty(entity);

        _appearance.SetData(entity, PowerDeviceVisuals.Powered, entity.Comp.Powered);
    }
}
