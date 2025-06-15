using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Vampire.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodEssenceComponent : Component
{
    [DataField]
    public float BloodEssence = 200.0f;

    /// <summary>
    /// Handles withdrawl of blood essence from this component.
    /// </summary>
    /// <param name="withdraw">The amount of BloodEssence to attempt to withdraw from the owner.</param>
    /// <returns>
    /// Returns a value between 0.0 and `withdraw` which corresponds to the amount of BloodEssence withdrawn.
    /// 0.0 being the minimum value if the owner is out of BloodEssence.
    /// </returns>
    public float Withdraw(float withdraw)
    {
        BloodEssence = Math.Max(0.0f, BloodEssence - withdraw);
        return BloodEssence;
    }
}
