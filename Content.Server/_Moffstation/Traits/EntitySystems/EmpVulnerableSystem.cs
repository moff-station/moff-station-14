using Content.Shared._Moffstation.Traits.EntitySystems;
using Content.Shared._Moffstation.Traits.Components;
using Content.Shared.Emp;

namespace Content.Server._Moffstation.Traits.EntitySystems;

public sealed class EmpVulnerableSystem : SharedEmpVulnerableSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmpVulnerableComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnEmpPulse(Entity<EmpVulnerableComponent> entity, ref EmpPulseEvent ev)
    {
        Disrupt(entity, entity.Comp.EmpStunDuration);
    }
}
