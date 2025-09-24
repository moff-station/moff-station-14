using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Traits;

/// <summary>
/// Entities with this component will have debilitating effects applied to them when under the effects of a recent ion storm or EMP
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmpVulnerableComponent: Component
{
    /// <summary>
    /// Time the entity will be disrupted from an ion storm
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan IonStunDuration = TimeSpan.FromSeconds(5);

    ///<summary>
    /// Time the entity will be disrupted from an EMP
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan EmpStunDuration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Movement speed multiplier for slowing down the target while they are disrupted, assuming they are not stunned
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SlowTo = 0.5f;
}
