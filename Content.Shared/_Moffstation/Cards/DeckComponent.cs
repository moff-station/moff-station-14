using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Cards;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DeckComponent : Component
{
    [DataField("containerId"), AutoNetworkedField]
    public string ContainerId { get; set; } = "deck";

    [DataField("isFaceUp"), AutoNetworkedField]
    public bool IsFaceUp { get; set; } = false;
}
