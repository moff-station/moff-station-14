using Content.Shared.DoAfter;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Tools.Components;

/// <summary>
/// This is used for entities with <see cref="ToolComponent"/> that are additionally
/// able to modify tiles.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedToolSystem))]
public sealed partial class ToolTileCompatibleComponent : Component
{
    /// <summary>
    /// The time it takes to modify the tile.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Whether or not the tile being modified must be unobstructed
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequiresUnobstructed = true;

    // Moffstation - Begin - Fireaxe can destroy hull plating
    /// <summary>
    /// If not null, provide audio and visual indicators emitted when the tool is being used on a tile.
    /// </summary>
    [DataField("cues")]
    public ToolTileAudioVisualCues? AudioVisualCues;
    // Moffstation - End
}

// Moffstation - Begin - Fireaxe can remove plating
[DataRecord]
public sealed partial record ToolTileAudioVisualCues
{
    public SoundSpecifier? StartSound;
    public LocId? StartPopup;
    public SoundSpecifier? EndSound;
}
// Moffstation - End


[Serializable, NetSerializable]
public sealed partial class TileToolDoAfterEvent : DoAfterEvent
{
    public NetEntity Grid;
    public Vector2i GridTile;

    public TileToolDoAfterEvent(NetEntity grid, Vector2i gridTile)
    {
        Grid = grid;
        GridTile = gridTile;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }

    public override bool IsDuplicate(DoAfterEvent other)
    {
        return other is TileToolDoAfterEvent otherTile
               && Grid == otherTile.Grid
               && GridTile == otherTile.GridTile;
    }
}
