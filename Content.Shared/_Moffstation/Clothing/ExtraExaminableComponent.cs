using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Clothing;

/// <summary>
/// Clothing that contributes to the wearer's examine message when worn
/// "wearer" is the default if you need to get info for the person interacting with the item
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ExtraExaminableComponent : Component
{
    /// <summary>
    /// Text to be displayed when the item is examined by itself
    /// </summary>
    [DataField]
    public LocId? ExaminedText = null;

    /// <summary>
    /// Localization ID of the examine text to display when the item is being worn.
    /// </summary>
    [DataField]
    public LocId? WornText = null;

    /// <summary>
    /// Text to be displayed when the item is being held
    /// </summary>
    [DataField]
    public LocId? HeldText = null;

    /// <summary>
    /// Only adds the examine text in the given slots. Only effects WornText.
    /// </summary>
    [DataField]
    public SlotFlags AllowedSlots = SlotFlags.WITHOUT_POCKET;
}

[ByRefEvent]
public readonly record struct HeldItemsAdditionalExamineEvent(EntityUid ExaminedEntity);
