using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Silicons.LawConfigurator;

/// <summary>
/// This is used for items that can change silicon laws on contact
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LawConfiguratorComponent : Component
{
    /// <summary>
    /// Sound that will be emitted on successful reprogramming
    /// </summary>
    [DataField]
    public SoundSpecifier? SuccessSound = null;

    /// <summary>
    /// Sound that will be emitted on failed reprogramming
    /// </summary>
    [DataField]
    public SoundSpecifier? FailureSound = null;

    /// <summary>
    /// Id of the item slot containing the law board to be transfered.
    /// </summary>
    [DataField]
    public string LawBoardSlot = "law_board_slot";
}
