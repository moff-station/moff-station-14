using Content.Shared._Moffstation.Temperature.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Temperature.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Temperature.Components;

/// <summary>
/// Sets <see cref="AppearanceComponent">appearance data</see> based on <see cref="TemperatureComponent.Temperature"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(TemperatureVisualsSystem))]
public sealed partial class TemperatureVisualsComponent : Component, ISerializationHooks
{
    /// <summary>
    /// The <see cref="AppearanceComponent">appearance data</see> to set <see cref="TemperatureVisuals.Key"/> to based
    /// on this entity's <see cref="TemperatureComponent.Temperature"/>. The entry whose key is lower than or equal to
    /// the entity's temperature is what will be set.
    /// </summary>
    [DataField("visuals", required: true)]
    private Dictionary<FixedPoint2, string?> _visuals = new();

    /// <inheritdoc cref="_visuals"/>
    [ViewVariables]
    public SortedDictionary<FixedPoint2, string?> Visuals;

    /// <inheritdoc/>
    void ISerializationHooks.AfterDeserialization()
    {
        Visuals = new SortedDictionary<FixedPoint2, string?>(_visuals);
    }
}

/// <summary>
/// Enum keys for setting sprite visuals related to <see cref="TemperatureComponent"/>.
/// </summary>
[Serializable, NetSerializable]
public enum TemperatureVisuals : byte
{
    Key,
    Layer,
}
