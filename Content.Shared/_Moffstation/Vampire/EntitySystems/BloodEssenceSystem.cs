using Content.Shared._Moffstation.Vampire.Components;

namespace Content.Shared._Moffstation.Vampire.EntitySystems;

/// <summary>
/// The system handling withdrawal of blood essence from entities with the BloodEssence component
/// </summary>
public sealed class BloodEssenceSystem : EntitySystem
{
    /// <summary>
    /// Handles withdrawal of blood essence from this component.
    /// </summary>
    /// <param name="entity">The entity which owns the blood essence component.</param>
    /// <param name="withdraw">The amount of blood essence to attempt to withdraw from the owner.</param>
    /// <returns>
    /// Returns a value between 0.0 and <see cref="withdraw"/> which corresponds to the amount of blood essence withdrawn.
    /// 0.0 being the minimum value if the owner is out of blood essence.
    /// </returns>
    public float Withdraw(Entity<BloodEssenceComponent> entity, float withdraw)
    {
        if (!TryComp<BloodEssenceComponent>(entity, out var comp))
            return 0.0f;
        if (comp.BloodEssence < withdraw)
        {
            var withdrawn = comp.BloodEssence;
            comp.BloodEssence = 0.0f;
            return withdrawn;
        }
        comp.BloodEssence -= withdraw;
        return withdraw;
    }
}
