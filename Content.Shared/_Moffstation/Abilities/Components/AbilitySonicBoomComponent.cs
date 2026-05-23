using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Abilities.Components;

/// <summary>
/// This component gives the entity it's attached to the action to perform the "sonic boom" attack with the included parameters.
/// </summary>
[RegisterComponent]
public sealed partial class AbilitySonicBoomComponent : Component
{
    /// <summary>
    /// Radius of the sonic boom effect.
    /// </summary>
    [DataField]
    public float FlingRadius = 1.0f;

    /// <summary>
    /// The strength of the fling at the center of the boom.
    /// This scales linearly with the distance the object has to the entity.
    /// </summary>
    [DataField]
    public float FlingStrength = 1.5f;

    /// <summary>
    /// Slow down percentage during the casting of the sonic boom.
    /// </summary>
    [DataField]
    public float Slowdown = 0.5f;

    /// <summary>
    /// Slow down duration during the casting of the sonic boom.
    /// </summary>
    [DataField]
    public TimeSpan SlowdownDuration = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The sound effect to play when the sonic boom is performed.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Grenades/Supermatter/supermatter_start.ogg");

    /// <summary>
    /// The prototype of the action given to the entity with this component.
    /// </summary>
    [DataField]
    public EntProtoId ActionProto = "ActionSonicBoom";

    /// <summary>
    /// A place to store the action entity.
    /// </summary>
    [DataField]
    public EntityUid? Action;

    public override bool SendOnlyToOwner => true;
}
