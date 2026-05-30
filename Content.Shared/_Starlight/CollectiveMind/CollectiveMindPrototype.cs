using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.CollectiveMind;

[Prototype]
public sealed partial class CollectiveMindPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; set;  } = default!;

    [DataField]
    public string Name = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);

    [DataField("keycode", required: true)]
    public char KeyCode;

    [DataField]
    public Color Color = Color.Lime;

    /// If an entity passes any whitelist in this list, they get access to this collective mind.
    [DataField(required: true)]
    public List<EntityWhitelist> Requirements = new();
}
