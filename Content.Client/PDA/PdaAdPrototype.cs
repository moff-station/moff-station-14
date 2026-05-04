using Robust.Shared.Prototypes;

namespace Content.Client.PDA;

[Prototype]
public sealed partial class PdaAdPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; private set; } = default!;

    //[DataField(required: true)]
    [DataField]
    public string SpriteState { get; private set; } = default!;

    //[DataField(required: true)]
    //public SpriteSpecifier Icon { get; private set; } = default!;

    //[DataField]
    //public LocId Name { get; private set; } = "PdaAd-component-name";

    //[DataField]
    //public LocId Description { get; private set; }

    [DataField]
    public bool Hidden { get; private set; }
}
