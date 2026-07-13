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
    /// <see cref="WeightedRandomPrototype"/> for a random lawset to possibly replace the old one with.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<WeightedRandomPrototype> StartingLawset = "IonStormLawsets";

    /// <summary>
    /// Chance to remove a random law.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float RemoveChance = 0.2f;

    /// <summary>
    /// Chance to replace a random law with a new one
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ReplaceChance = 0.2f;

    /// <summary>
    /// Chance to shuffle laws after everything is done.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ShuffleChance = 0.2f;
}
