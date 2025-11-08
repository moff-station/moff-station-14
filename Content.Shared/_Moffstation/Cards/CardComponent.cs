using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Cards;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CardComponent : Component
{
    [DataField("suit"), AutoNetworkedField]
    public string Suit { get; set; } = "Special";

    [DataField("value"), AutoNetworkedField]
    public string Value { get; set; } = "Blank";

    [DataField("suitOrder"), AutoNetworkedField]
    public int SuitOrder { get; set; } = 0;

    [DataField("valueOrder"), AutoNetworkedField]
    public int ValueOrder { get; set; } = 0;

    [DataField("isFaceUp"), AutoNetworkedField]
    public bool IsFaceUp { get; set; } = true;

    [DataField("headSlotPrivacy"), AutoNetworkedField]
    public bool HeadSlotPrivacy { get; set; } = false;

    [DataField("frontRsi"), AutoNetworkedField]
    public string? FrontRsi { get; set; }

    [DataField("frontState"), AutoNetworkedField]
    public string? FrontState { get; set; }

    [DataField("backRsi"), AutoNetworkedField]
    public string? BackRsi { get; set; }

    [DataField("backState"), AutoNetworkedField]
    public string? BackState { get; set; }
}
