using Content.Server.Cargo.Components;

namespace Content.Server._Moffstation.Pirate.Components;

/// <summary>
/// Any entities intersecting when a shuttle is recalled will be sold.
/// </summary>

[RegisterComponent]
public sealed partial class PiratePalletComponent : Component
{
    [DataField]
    public BuySellType PalletType;
}
