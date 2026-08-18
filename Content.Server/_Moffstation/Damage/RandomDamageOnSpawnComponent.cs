using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Damage;

[RegisterComponent]
public sealed partial class RandomDamageOnSpawnComponent : Component
{
    [DataField]
    public List<ProtoId<DamageTypePrototype>>? DamageTypes;

    [DataField]
    public float Min;

    [DataField(required: true)]
    public float Max;
}
