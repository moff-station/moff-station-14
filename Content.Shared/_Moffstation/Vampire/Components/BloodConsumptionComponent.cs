using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Vampire.Components;

/// <summary>
/// This is the component given to vampires and other creatures which utilize blood as their main form of sustenance.
/// Interfacing largely with the entity's bloodstream, this component treats their bloodstream as their hunger value, and
/// main form of healing.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodConsumptionComponent : Component
{
    /// <summary>
    /// The next time an update will be performed
    /// </summary>
    [DataField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// The interval between updates.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The reagent prototype to use for this entity's blood.
    /// This is done to ensure we overwrite the entity's starting bloodstream reagent.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> BloodReagent = "Blood";

    /// <summary>
    /// The amount of blood to lose per update. In standard units (u).
    /// </summary>
    [DataField]
    public float BaseBloodlossPerUpdate = -0.5f;

    /// <summary>
    /// The amount of blood to lose per update while restoring health naturally. In standard units (u).
    /// </summary>
    [DataField]
    public float HealingBloodlossPerUpdate = -5.0f;

    /// <summary>
    /// Damage to deal per update (use negative values to specify healing)
    /// </summary>
    [DataField]
    public DamageSpecifier HealPerUpdate = new();

    /// <summary>
    /// Previous blood percentage.
    /// This is mostly used to know how much we need to adjust the thirst and hunger values between updates since
    /// we don't have any other way to track that.
    /// </summary>
    [DataField]
    public float PrevBloodPercentage = 0.5f;

    /// <summary>
    /// The maximum percentage (0.0-1.0) of change of hunger and thirst per update.
    /// </summary>
    [DataField]
    public float MaxChange = 0.1f;

    public override bool SendOnlyToOwner => true;
}
