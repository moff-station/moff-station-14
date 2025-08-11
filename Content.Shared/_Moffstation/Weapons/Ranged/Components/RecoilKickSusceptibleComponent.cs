using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Weapons.Ranged.Components;

[DataDefinition]
public sealed partial class RecoilKick
{
    /// <summary>
    /// The time (in seconds) a wielder will be airborne when affected by recoil kick.
    /// </summary>
    public const float FlyTime = 0.2f;

    /// <summary>
    /// The impulse (in Newton-seconds) this gun applies to wielders who are
    /// <see cref="RecoilKickSusceptibleComponent">susceptible to recoil kick</see> when it is fired.
    /// </summary>
    [DataField]
    public float Impulse;

    /// <summary>
    /// Stamina damage applied per m/s applied to gun wielder when kicked by recoil.
    /// </summary>
    [DataField]
    public float StaminaMultiplier = 10;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RecoilKickSusceptibleComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public float MassFactor = 1.0f;
}
