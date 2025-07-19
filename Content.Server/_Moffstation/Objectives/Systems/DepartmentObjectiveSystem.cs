using Content.Server._Moffstation.Objectives.Components;
using Content.Shared.Objectives.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Objectives.Systems;

/// <summary>
/// Provides API for overrideing what department will get selected in an objective
/// </summary>
public sealed class DepartmentObjectiveSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DepartmentObjectiveComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }

    private void OnAfterAssign(Entity<DepartmentObjectiveComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        if (ent.Comp.Target is not {} target)
            return;

        var departmentName = Loc.GetString(_protoMan.Index(target).Name);
        _metaData.SetEntityName(ent.Owner, Loc.GetString(ent.Comp.Title, ("$targetName", departmentName)), args.Meta);
    }
}
