using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.PizzaScurret.Receipt;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PizzaReceiptComponent : Component
{
    /// <summary>
    /// The the signature written when a paper is signed
    /// </summary>
    [DataField, AutoNetworkedField]
    public string DetectedSignature = "";

    [DataField, AutoNetworkedField]
    public string RequiredSignature = "";
}
