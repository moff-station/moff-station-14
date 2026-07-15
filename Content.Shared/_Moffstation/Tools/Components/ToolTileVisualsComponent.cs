using Content.Shared.Tools;
using Robust.Shared.Audio;

namespace Content.Shared._Moffstation.Tools.Components;

/// <summary>
/// This is used to add audio and visual effects when a tool is used to interact with a tile.
/// </summary>
[RegisterComponent]
public sealed partial class ToolTileVisualsComponent : Component
{
    /// <summary>
    /// If not null, the sound played at the start of the tile interaction DoAfter
    /// </summary>
    [DataField]
    public SoundSpecifier? InteractionStartSound = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg");

    /// <summary>
    /// If not null, the sound played at the end of the tile interaction DoAfter (only if the DoAfter succeeded)
    /// </summary>
    [DataField]
    public SoundSpecifier? InteractionEndSound = new SoundPathSpecifier("/Audio/Items/crowbar.ogg");

    /// <summary>
    /// If not null, a popup appearing for the entity at the start of the tile interaction DoAfter
    /// </summary>
    [DataField]
    public string InteractionStartSelfPopup = "hull_prying_self_popup";

    /// <summary>
    /// If not null, a popup appearing for others entities at the start of the tile interaction DoAfter
    /// </summary>
    [DataField]
    public string InteractionStartOthersPopup = "hull_prying_other_popup";
}
