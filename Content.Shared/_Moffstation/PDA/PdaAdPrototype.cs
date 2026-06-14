using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Random;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.PDA;

[Prototype]
public sealed partial class PdaAdPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SpriteSpecifier Sprite = default!;

    /// <summary>
    /// A weighted random prototype for how rare each advertisement should be.
    /// </summary>
    public static readonly ProtoId<WeightedRandomPrototype> AdWeightPrototype = "AdWeights";
}
