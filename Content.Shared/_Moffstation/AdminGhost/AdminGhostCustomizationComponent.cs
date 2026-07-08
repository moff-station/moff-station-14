using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.AdminGhost;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class AdminGhostCustomizationComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId? SpritePrototype;

    [DataField, AutoNetworkedField]
    public string? CustomName;

    [DataField, AutoNetworkedField]
    public string? CustomDescription;

    [DataField, AutoNetworkedField]
    public float? WalkSpeed;

    [DataField, AutoNetworkedField]
    public float? SprintSpeed;

    [DataField, AutoNetworkedField]
    public MapTextData? MapText;

    [DataField, AutoNetworkedField]
    public bool ShowJobIcons;

    [DataField, AutoNetworkedField]
    public bool ShowCriminalRecordIcons;

    [DataField, AutoNetworkedField]
    public bool ShowMindShieldIcons;

    [DataField, AutoNetworkedField]
    public bool ShowSyndicateIcons;

    [DataField, AutoNetworkedField]
    public bool ShowHealthBars;

}

[DataDefinition, NetSerializable, Serializable]
public sealed partial class MapTextData
{
    [DataField]
    public string? Text;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public int FontSize = 12;

    [DataField]
    public Vector2 Offset = Vector2.Zero;
}
