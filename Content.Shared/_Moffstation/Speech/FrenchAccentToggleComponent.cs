using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Speech;

[RegisterComponent]
public sealed partial class FrenchAccentToggleComponent : Component
{
    /// <summary>
    /// Variable pointing at the Alert modal
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> ToggleAlertProtoId = "FrenchToggle";
}

public sealed partial class ToggleFrenchAccentEvent : BaseAlertEvent;
