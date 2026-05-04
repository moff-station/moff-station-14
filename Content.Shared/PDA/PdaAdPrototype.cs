using Robust.Shared.Prototypes;

namespace Content.Shared.PDA;

[Prototype]
public sealed partial class PdaAdPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; private set; } = default!;

    //[DataField(required: true)]
    [DataField(required: true)]
    public string SpriteState { get; private set; } = default!;

    [DataField(required: true)]
    public int Weight { get; private set; } = default!;

    [DataField]
    public bool Hidden { get; private set; }
}
