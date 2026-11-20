using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Power;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Content.Shared.Wires;

namespace Content.Server.Silicons.StationAi;

/// <summary>
/// Handles StationAiVision functionality for the attached entity.
/// </summary>
public sealed partial class AiVisionWireAction : ComponentWireAction<StationAiVisionComponent>
{
    public override string Name { get; set; } = "wire-name-ai-vision-light";
    public override Color Color { get; set; } = Color.White;
    public override object StatusKey => AirlockWireStatus.AiVisionIndicator;

    // Moffstation - hacker remote
    [DataField("pulseTimeout")]
    private int _pulseTimeout = 30;
    // Moffstation - end

    public override StatusLightState? GetLightState(Wire wire, StationAiVisionComponent component)
    {
        return component.Enabled ? StatusLightState.On : StatusLightState.Off;
    }

    public override bool Cut(EntityUid user, Wire wire, StationAiVisionComponent component)
    {
        return EntityManager.System<SharedStationAiSystem>()
            .SetVisionEnabled((wire.Owner, component), false, announce: true);
    }

    public override bool Mend(EntityUid user, Wire wire, StationAiVisionComponent component)
    {
        // Moffstation - hacker remote
        WiresSystem.TryCancelWireAction(wire.Owner, StationAiVisionWireKey.PulseCancel);
        WiresSystem.SetData(wire.Owner, StationAiVisionWireKey.Pulsed, false);
        // Moffstation - end
        return EntityManager.System<SharedStationAiSystem>()
            .SetVisionEnabled((wire.Owner, component), true);
    }

    public override void Pulse(EntityUid user, Wire wire, StationAiVisionComponent component)
    {
        // Moffstation - hacker remote
        if (WiresSystem.TryGetData<bool>(wire.Owner, StationAiVisionWireKey.Pulsed, out var pulsedKey) && pulsedKey)
            return;

        WiresSystem.SetData(wire.Owner, StationAiVisionWireKey.Pulsed, true);

        EntityManager.System<SharedStationAiSystem>()
            .SetVisionEnabled((wire.Owner, component), false);

        WiresSystem.StartWireAction(wire.Owner, _pulseTimeout, StationAiVisionWireKey.PulseCancel, new TimedWireEvent(x => AwaitPulseCancel(x, component), wire));
        // Moffstation - end
    }

    private void AwaitPulseCancel(Wire wire, StationAiVisionComponent component)
    {
        WiresSystem.SetData(wire.Owner,  StationAiVisionWireKey.Pulsed, false);
        EntityManager.System<SharedStationAiSystem>()
            .SetVisionEnabled((wire.Owner, component), true);
    }
}
