using Content.Shared._Moffstation.Sparks.Components;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Sparks.Components;

/// <summary>
/// An entity that sparks when triggered
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSparksSystem))]
// Moffstation - Begin - Make ESBaseSparkConfigurationComponent an interface
public sealed partial class ESSparkOnTriggerComponent : BaseXOnTriggerComponent, ESBaseSparkConfigurationComponent
{
    [IncludeDataField]
    public ESBaseSparkConfigurationComponent.Config SparkConfig { get; set; } = new();
}
// Moffstation - End
