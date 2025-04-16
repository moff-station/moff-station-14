using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Revolutionary.Components;
using Robust.Shared.Random;
using System.Linq;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Systems;

public sealed class PickDepartmentTargetSystem : EntitySystem
{
    [Dependency] private readonly TargetDepartmentSystem _target = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;
    [Dependency] private readonly SharedJobSystem _jobSystem = default!;
    [Dependency] private readonly PrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PickRandomDepartmentComponent, ObjectiveAssignedEvent>(OnRandomDepartmentAssigned);
    }

    private void OnRandomDepartmentAssigned(Entity<PickRandomDepartmentComponent> ent, ref ObjectiveAssignedEvent args)
    {
        if (!TryComp<TargetDepartmentComponent>(ent.Owner, out var person))
        {
            args.Cancelled = true;
            return;
        }
        var departments = _prototypeManager.EnumeratePrototypes<DepartmentPrototype>().ToArray();

        _target.SetTarget(_random.Pick(departments).Name, ent);
    }
}
