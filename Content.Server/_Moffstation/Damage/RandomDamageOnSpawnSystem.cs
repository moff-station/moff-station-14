using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.Damage;

public sealed partial class RandomDamageOnSpawnSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RandomDamageOnSpawnComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, RandomDamageOnSpawnComponent component, MapInitEvent args)
    {
        var types = component.DamageTypes ?? GetAllDamageTypes();

        var remaining = _random.NextFloat(component.Min, component.Max);
        var damage = new DamageSpecifier();

        while (remaining > 0f)
        {
            var type = _random.Pick(types);
            var amount = remaining <= 10.0f ? remaining : _random.NextFloat(1.0f, remaining);

            damage.DamageDict.TryGetValue(type, out var current);
            damage.DamageDict[type] = current + amount;
            remaining -= amount;
        }

        _damageable.SetDamage(uid, damage);
    }

    private List<ProtoId<DamageTypePrototype>> GetAllDamageTypes()
    {
        var damageTypes = new List<ProtoId<DamageTypePrototype>>();

        foreach (var proto in _prototype.EnumeratePrototypes<DamageTypePrototype>())
        {
            damageTypes.Add(proto.ID);
        }
        return damageTypes;
    }
}
