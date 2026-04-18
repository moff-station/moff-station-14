using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Robotics.LawProgrammer;

/// <summary>
/// This is used for items that can change silicon laws on contact
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LawProgrammerComponent : Component
{
    /// <summary>
    /// Duration of the DoAfter without any modifiers.
    /// </summary>
    [DataField]
    public TimeSpan BaseAttemptDuration = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    /// Sound that will be emitted on attempt
    /// </summary>
    [DataField]
    public SoundSpecifier? AttemptSound = null;

    /// <summary>
    /// Sound that will be emitted on successful attempt
    /// </summary>
    [DataField]
    public SoundSpecifier? SuccessSound = null;

    /// <summary>
    /// Sound that will be emitted on failed attempt
    /// </summary>
    [DataField]
    public SoundSpecifier? FailureSound = null;

    /// <summary>
    /// Id of the item slot containing the law board to be transfered.
    /// </summary>
    [DataField]
    public string LawBoardSlot = "law_board_slot";

    /// <summary>
    /// FOR TESTING PURPOSES ON DEV ENVIRONMENT.
    /// Does the target require a mind to be reprogrammed.
    /// </summary>
    [DataField]
    public bool RequireMind = true;
}

/// <summary>
/// This is used for entity that react to contact with a LawReprogrammer component.
/// </summary>
[RegisterComponent]
public sealed partial class LawProgrammerTargetComponent : Component
{
    /// <summary>
    /// Indicate if the entity is immune to reprogramming attempts.
    /// </summary>
    [DataField]
    public bool IsImmune = false;

    /// <summary>
    /// Multiplicative factor of the doAfter duration when attempting to configure this entity
    /// </summary>
    [DataField]
    public float DurationMultiplier = 1f;
}
