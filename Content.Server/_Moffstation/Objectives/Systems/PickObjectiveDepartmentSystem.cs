using System.Linq;
using Content.Server._Moffstation.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._Moffstation.Objectives.Systems;

/// <summary>
/// Handles assinging a target to an objective entity with <see cref="TargetDepartmentComponent"/> using different components.
/// </summary>
public sealed class PickObjectiveDepartmentSystem : EntitySystem
{
    [Dependency] private readonly TargetDepartmentSystem _target = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PickRandomDepartmentComponent, ObjectiveAssignedEvent>(OnRandomDepartmentAssigned);

    }

    private void OnRandomDepartmentAssigned(Entity<PickRandomDepartmentComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid objective prototype
        if (!TryComp<TargetDepartmentComponent>(ent.Owner, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;


        var departments = new List<DepartmentPrototype>();
        // generate the list of valid departments that can be selected
        foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>().ToList())     // Remove invalid departments
        {
            switch (department.ID)
            {
                case "CentralCommand":
                case "Silicon":
                case "Specific":
                    continue;
            }

            if (ent.Comp.DepartmentBlacklist.Contains(department.ID))
                continue;
            if (ent.Comp.DepartmentWhitelist.Count != 0 && !ent.Comp.DepartmentWhitelist.Contains(department.ID))
                continue;

            if (!ent.Comp.AllowSameDepartment &&
                _jobs.MindTryGetJob(args.MindId, out var job) &&
                _jobs.TryGetDepartment(job.ID, out var workedDepartment) &&
                workedDepartment.ID == department.ID)
                continue;

            departments.Add(department);
        }
        _target.SetTarget(ent.Owner, Loc.GetString(_random.Pick(departments).Name), target);
    }
}
