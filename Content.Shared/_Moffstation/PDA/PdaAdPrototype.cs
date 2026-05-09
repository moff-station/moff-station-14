using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.PDA;

[Prototype]
public sealed partial class PdaAdPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SpriteSpecifier Sprite = default!;

    [DataField(required: true)]
    public int Weight = default!;

    [DataField]
    public bool Hidden = default!;
}
