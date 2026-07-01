using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.AdminGhost;

[Serializable, NetSerializable]
public sealed class AdminGhostSavedData
{
    public string? SpritePrototype { get; set; }
    public string? CustomName { get; set; }
    public string? CustomDescription { get; set; }
    public float? WalkSpeed { get; set; }
    public float? SprintSpeed { get; set; }
    public SavedMapTextData? MapText { get; set; }
    public bool ShowJobIcons { get; set; }
    public bool ShowCriminalRecordIcons { get; set; }
    public bool ShowMindShieldIcons { get; set; }
    public bool ShowSyndicateIcons { get; set; }
    public bool ShowHealthBars { get; set; }
}

[Serializable, NetSerializable]
public sealed class SavedMapTextData
{
    public string? Text { get; set; }
    public string? ColorHex { get; set; }
    public int? FontSize { get; set; }
    public Vector2? Offset { get; set; }
}
