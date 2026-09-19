using Content.Shared._Moffstation.Temperature.Systems;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Temperature.Components;

/// <summary>
/// This component heats up entities in a container.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ContainedEntityHeaterSystem))]
public sealed partial class ContainedEntityHeaterComponent : Component
{
    /// <summary>
    /// The container whose contents will be heated.
    /// </summary>
    [DataField("container", required: true)]
    public string ContainerId;

    [ViewVariables(VVAccess.ReadOnly)]
    public BaseContainer Container;

    /// <summary>
    /// The thermal power added to entities in the container, distributed evenly across them all, measured in Watts.
    /// </summary>
    [DataField(required: true)]
    public float Power;

    /// <summary>
    /// If true, ignores insulative effects. This is just naively passed on to <see cref="SharedTemperatureSystem.ChangeHeat"/>.
    /// </summary>
    [DataField]
    public bool IgnoreHeatResistance;
}
