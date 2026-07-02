using Content.Shared._Moffstation.Temperature.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Temperature.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Temperature.Components;

/// <summary>
/// Sets <see cref="AppearanceComponent">appearance data</see> keyed by <see cref="TemperatureVisuals.Key"/> based
/// on this entity's <see cref="TemperatureComponent.CurrentTemperature"/>. The data set is specified in
/// <see cref="Visuals"/> such that the entry whose key is just lower or equal to the entity's current temperature is
/// used.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(TemperatureVisualsSystem))]
public sealed partial class TemperatureVisualsComponent : Component
{
    [DataField(required: true)]
    public Dictionary<FixedPoint2, string?> Visuals = new();

    /// <summary>
    /// <see cref="Visuals"/>, sorted by key for efficient lookup later.
    /// </summary>
    public SortedDictionary<FixedPoint2, string?> VisualsSorted;
}

[Serializable, NetSerializable]
public enum TemperatureVisuals : byte
{
    Key,
    Layer,
}
