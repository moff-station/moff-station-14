using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Vampire.Components;

/// <summary>
/// This is the component which handles an entity's BloodEssence amount if they are a BloodEssence user.
/// The primary users of this component right now are vampires however this could be extended to others in the future.
/// This component does require the <see cref="Content.Shared._Moffstation.Vampire.Abilities.Components.AbilityFeedComponent"/>
/// ability so the entity can actually feed off of blood and obtain blood essence.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodEssenceUserComponent : Component
{
    /// <summary>
    /// The vampire's current amount of blood essence collected.
    /// </summary>
    [DataField]
    public float BloodEssenceTotal;

    /// <summary>
    /// The reagent prototype whitelist to use when determining which types of blood are valid for
    /// gaining BloodEssence from
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> BloodWhitelist = new();

    /// <summary>
    /// A dictionary of Entities this entity has fed from and the corresponding amounts.
    /// </summary>
    [DataField]
    public Dictionary<EntityUid, float> FedFrom = new();

}
