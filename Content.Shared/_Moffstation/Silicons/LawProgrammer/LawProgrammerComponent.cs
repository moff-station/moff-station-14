using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Silicons.LawProgrammer;

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
    public TimeSpan BaseAttemptDuration = TimeSpan.FromSeconds(1);

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
}
