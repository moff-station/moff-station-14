using Content.Shared.FixedPoint;
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Trigger.Components.Effects;

public abstract partial class TriggerOnTemperatureComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// The set temperature against which this entity's temperature is compared.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 Temperature;
}

/// <inheritdoc cref="TriggerOnTemperatureComponent" />
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnTemperatureAboveComponent : TriggerOnTemperatureComponent;

/// <inheritdoc cref="TriggerOnTemperatureComponent" />
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnTemperatureBelowComponent : TriggerOnTemperatureComponent;
