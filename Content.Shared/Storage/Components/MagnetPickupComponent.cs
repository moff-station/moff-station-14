using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

/// <summary>
/// Applies an ongoing pickup area around the attached entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[AutoGenerateComponentPause]
public sealed partial class MagnetPickupComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("nextScan")]
    [AutoPausedField]
    [AutoNetworkedField]
    public TimeSpan NextScan = TimeSpan.Zero;

    /// <summary>
    /// What container slot the magnet needs to be in to work.
    /// Moffstation: Null indicates the magnet pickup functionality should always be active.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("slotFlags")]
    public SlotFlags? SlotFlags = Inventory.SlotFlags.BELT; // Moffstation - Enable magnet pickup in any slot

    [ViewVariables(VVAccess.ReadWrite), DataField("range")]
    public float Range = 1f;
}
