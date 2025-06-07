using Robust.Shared.Prototypes;

namespace Content.Shared.CollectiveMind;

[Prototype]
public sealed partial class CollectiveMindPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; } = default!;

    [DataField]
    public string Name = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);

    [DataField("keycode", required: true)]
    public char KeyCode;

    [DataField]
    public Color Color = Color.Lime;

    [DataField]
    public List<string> RequiredComponents = [];

    [DataField]
    public List<ProtoId<Tag.TagPrototype>> RequiredTags = [];
}
