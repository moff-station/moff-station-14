using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.StationEvents.Components;

/// <summary>
/// See <see cref="IonAttackRule"/>
/// </summary>
[RegisterComponent]
public sealed partial class IonAttackRuleComponent : Component
{
    /// <summary>
    /// <see cref="WeightedRandomPrototype"/>, a random starting lawset
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<WeightedRandomPrototype> StartingLawset = "IonStormLawsets";

    /// <summary>
    /// Chance to remove a random law from the starting lawset
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float RemoveChance = 0.2f;

    /// <summary>
    /// Chance to replace a random law from the starting lawset with a new one
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ReplaceChance = 0.2f;

    /// <summary>
    /// Chance to shuffle the laws of the starting lawset
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ShuffleChance = 0.2f;
}
