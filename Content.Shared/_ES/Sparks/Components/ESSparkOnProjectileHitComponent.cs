using Content.Shared._Moffstation.Sparks.Components;
using Content.Shared.Projectiles;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Sparks.Components;

/// <summary>
/// An entity with <see cref="ProjectileComponent"/> that sparks when hitting something
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSparksSystem))]
// Moffstation - Begin - Make ESBaseSparkConfigurationComponent an interface
public sealed partial class ESSparkOnProjectileHitComponent : Component, ESBaseSparkConfigurationComponent
{
    [IncludeDataField]
    public ESBaseSparkConfigurationComponent.Config SparkConfig { get; set; } = new();
}
// Moffstation - End
