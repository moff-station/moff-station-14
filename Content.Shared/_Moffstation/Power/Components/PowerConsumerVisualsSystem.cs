using Content.Shared.Examine;
using Content.Shared.Power;

namespace Content.Shared._Moffstation.Power.Components;

/// This system implements the behavior of <see cref="PowerConsumerVisualsComponent"/>.
public abstract partial class PowerConsumerVisualsSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnExamined(Entity<PowerConsumerVisualsComponent> ent, ref ExaminedEvent args) => args.PushMarkup(
        Loc.GetString(
            "power-receiver-component-on-examine-main",
            (
                "stateText",
                Loc.GetString(
                    ent.Comp.Powered
                        ? "power-receiver-component-on-examine-powered"
                        : "power-receiver-component-on-examine-unpowered"
                )
            )
        ));
}
