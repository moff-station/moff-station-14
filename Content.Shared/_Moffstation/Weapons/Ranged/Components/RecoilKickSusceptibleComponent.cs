using Content.Shared.Inventory;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Weapons.Ranged.Components;

/// <summary>
/// This class defines how much of a kick guns can do to wielders with <see cref="RecoilKickSusceptibleComponent"/>.
/// </summary>
/// <seealso cref="GunComponent.RecoilKick"/>
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

/// <summary>
/// This makes an entity susceptible to <see cref="RecoilKick"/> when a gun is fired.
/// </summary>
/// <seealso cref="RecoilKick"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RecoilKickSusceptibleComponent : Component
{
    /// <summary>
    /// When calculating the effects of recoil kick, mass is multiplied by this factor. Specifically, this is usually a
    /// much smaller number, like 0.01 to really throw the wielder.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float MassFactor = 1.0f;
}

/// <summary>
/// This event is raised just before <see cref="RecoilKick"/> is applied to allow for the effect to be mitigated, eg. by
/// wearing magboots.
/// </summary>
[ByRefEvent]
public record struct RecoilKickAttemptEvent() : IInventoryRelayEvent
{
    /// <summary>
    /// Handlers of this event should modify this value, usually by multiplying it by a factor.
    /// </summary>
    public float ImpulseEffectivenessFactor = 1.0f;

    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}

