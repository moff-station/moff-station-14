using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Vampire.Abilities.Components;

[RegisterComponent]
public sealed partial class AbilityGlareComponent : Component
{
    [DataField]
    public float Range = 1.0f;

    [DataField]
    public float DamageFront = 70;

    [DataField]
    public float DamageSides = 35;

    [DataField]
    public float DamageRear = 10;

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(8);

    [DataField]
    public EntProtoId ActionProto = "ActionVampireGlare";

    [DataField]
    public EntityUid? Action;
}
