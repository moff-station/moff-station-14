using Content.Shared._ES.Core.Timer.Components;
using Content.Shared._Moffstation.Sparks.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Spawners;

namespace Content.Shared._ES.Sparks.Components;

/// <summary>
/// An entity with <see cref="TimedDespawnComponent"/> or <see cref="ESTimedDespawnComponent"/> that sparks when it despawns.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSparksSystem))]
// Moffstation - Begin - Make ESBaseSparkConfigurationComponent an interface
public sealed partial class ESSparkOnDespawnComponent : Component, ESBaseSparkConfigurationComponent
{
    [IncludeDataField]
    public ESBaseSparkConfigurationComponent.Config SparkConfig { get; set; } = new();
}
// Moffstation - End
