using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Vampire.Abilities.Components;

/// <summary>
/// This component handles the vampire's Glare ability.
/// When added to an entity, it will give them the Glare ability/spell, which is intended for use by
/// vampires as a method to both free themselves from custody, and possibly take down lone foes.
/// </summary>
/// <remarks>
/// todo:
///   - Check if the vampire is blindfolded and prevent usage if it is.
///   - If the vampire is knocked down, cause all directions to behave as though they are to the "side"
///   - Cause some sort of interaction with sunglasses and other flash protection. Don't make it totally
///     ineffective (as this is a somewhat magical ability) but definitely provide some decrease in the duration of stuns.
/// </remarks>
[RegisterComponent]
public sealed partial class AbilityGlareComponent : Component
{
    /// <summary>
    /// The radial distance around the vampire where the glare ability will hit.
    /// </summary>
    [DataField]
    public float Range = 1.0f;

    /// <summary>
    /// The amount of stamina damage to cause to the entity standing in front of the vampire.
    /// </summary>
    [DataField]
    public float DamageFront = 70;

    /// <summary>
    /// The amount of stamina damage to cause to the entity standing to the sides of the vampire.
    /// </summary>
    [DataField]
    public float DamageSides = 35;

    /// <summary>
    /// The amount of stamina damage to cause to the entity standing behind the vampire (should be rather small).
    /// </summary>
    [DataField]
    public float DamageRear = 10;

    /// <summary>
    /// The amount of time to knock down any entities to the front and sides of the vampire.
    /// </summary>
    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(2.0);

    /// <summary>
    /// The amount of stun time given to any entities to the front of the vampire.
    /// </summary>
    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(8);

    /// <summary>
    /// The Effect to spawn when the glare is performed.
    /// </summary>
    [DataField]
    public EntProtoId FlashEffectProto = "ReactionFlash";

    /// <summary>
    /// The sound effect to play when the glare is performed.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Moffstation/Effects/blind.ogg");

    /// <summary>
    /// The prototype of the action given to the entity with this component.
    /// </summary>
    [DataField]
    public EntProtoId ActionProto = "ActionVampireGlare";

    /// <summary>
    /// A place to store the action entity.
    /// </summary>
    [DataField]
    public EntityUid? Action;

    public override bool SendOnlyToOwner => true;
}
