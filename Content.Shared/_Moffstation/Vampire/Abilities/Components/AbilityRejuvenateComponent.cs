using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Vampire.Abilities.Components;

/// <summary>
/// This is the component which handles a vampire's rejuvenate ability. This ability is a method for them to recover from
/// stamina damage, stuns, and knockdowns. Upgraded versions will include additional healing as well.
/// </summary>
/// <remarks>
/// todo: Add upgrades.
/// </remarks>
[RegisterComponent]
public sealed partial class AbilityRejuvenateComponent : Component
{
    /// <summary>
    /// Amount of "damage" to be done to the vampire using rejuvenate. This value should be negative.
    /// </summary>
    [DataField]
    public float StamHealing = -100.0f;

    /// <summary>
    /// The amount of time to remove from any status effects affecting the vampire (stuns, knockdown).
    /// </summary>
    [DataField]
    public TimeSpan StatusEffectReductionTime = TimeSpan.FromSeconds(120);

    /// <summary>
    /// The sound to play when activating this ability.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Magic/forcewall.ogg");

    /// <summary>
    /// The action prototype to give to the entity with this component.
    /// </summary>
    [DataField]
    public EntProtoId ActionProto = "ActionVampireRejuvenate";

    /// <summary>
    /// A place to store the action entity.
    /// </summary>
    [DataField]
    public EntityUid? Action;

    public override bool SendOnlyToOwner => true;
}
