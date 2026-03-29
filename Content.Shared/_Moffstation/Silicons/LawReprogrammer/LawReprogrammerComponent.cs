using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Silicons.LawReprogrammer;

/// <summary>
/// This is used for items that can change silicon laws on contact
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class LawReprogrammerComponent : Component
{
    /// <summary>
    /// Minimum delay between consecutive uses.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public TimeSpan DelayBetweenUses = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Entities with this tag will be immune to the reprogrammer
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public ProtoId<TagPrototype> ImmuneTag =  "EmagImmune";


    [DataField]
    public string LawBoardSlot = "law_board_slot";

    /// <summary>
    /// Entity containing the laws that will be uploaded to the target on uses.
    /// </summary>
    public EntityUid? LawSource;

    public TimeSpan NextAllowedUsed = TimeSpan.Zero;
}
