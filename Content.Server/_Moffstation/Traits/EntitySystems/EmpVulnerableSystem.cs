using Content.Shared._Moffstation.Traits.EntitySystems;
using Content.Shared._Moffstation.Traits;
using Content.Server.Emp;

namespace Content.Server._Moffstation.Traits.EntitySystems;

public sealed class EmpVulnerableSystem: SharedEmpVulnerableSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmpVulnerableComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnEmpPulse(EntityUid uid, EmpVulnerableComponent component, EmpPulseEvent ev)
    {
        Disrupt(uid, component.EmpStunDuration);
    }
}
