using Content.Shared._Moffstation.Sparks.Components;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Sparks.Components;

/// <summary>
/// An entity with <see cref="ItemToggleComponent"/> that sparks when toggled
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSparksSystem))]
public sealed partial class ESSparkOnItemToggleComponent : Component, ESBaseSparkConfigurationComponent // Moffstation - Make ESBaseSparkConfigurationComponent an interface
{
    [IncludeDataField]
    public ESBaseSparkConfigurationComponent.Config SparkConfig { get; set; } = new(); // Moffstation - Make ESBaseSparkConfigurationComponent an interface

    /// <summary>
    /// If true, sparks will occur when the item is toggled ON.
    /// If false, sparks will occur when the item is toggled OFF.
    /// </summary>
    /// <remarks>
    /// There is no third option.
    /// </remarks>
    [DataField]
    public bool ActivatedSpark = true;
}
