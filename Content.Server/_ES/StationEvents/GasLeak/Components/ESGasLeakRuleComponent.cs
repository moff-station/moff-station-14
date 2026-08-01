using System.Numerics;
using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._ES.StationEvents.GasLeak.Components;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(ESGasLeakRule))]
public sealed partial class ESGasLeakRuleComponent : Component
{
    [DataField]
    public Gas LeakGas;

    [DataField]
    public float LeakRate;
    [DataField]
    public Vector2 LeakRateRange = new(80f, 200f);
    [DataField]
    public Vector2 LeakMolesRange = new(2000f, 4000f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextLeakTime;
    [DataField]
    public TimeSpan LeakDelay = TimeSpan.FromSeconds(1.5f);

    [DataField]
    public EntityUid LeakOrigin;

    // TODO: replace with real sparks
    [DataField]
    public SoundSpecifier? SparkSound = new SoundCollectionSpecifier("sparks");
    [DataField]
    public float SparkChance = 0.05f;
}
