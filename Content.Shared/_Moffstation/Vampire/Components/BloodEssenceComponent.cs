namespace Content.Shared._Moffstation.Vampire.Components;

/// <summary>
/// This component tracks the amount of BloodEssence any particular entity has.
/// The intended use for this is to give it to people who have been fed on by an entity
/// with <see cref="Vampire.Components.BloodEssenceUserComponent"/> using
/// the <see cref="Vampire.Abilities.Components.AbilityFeedComponent"/> in order to track the total Blood Essence
/// that entity has left.
/// </summary>
[RegisterComponent]
public sealed partial class BloodEssenceComponent : Component
{
    /// <summary>
    /// The total BloodEssence this entity has left.
    /// </summary>
    [DataField]
    public float BloodEssence = 200.0f;
}
