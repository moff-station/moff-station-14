using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.GameObjects;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Provides API for other components and handles setting the title.
/// </summary>
public sealed class TargetDepartmentSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TargetDepartmentComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }

    private void OnAfterAssign(EntityUid uid, TargetDepartmentComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        if (!GetTarget(uid, out var target, comp))
            return;

        // _metaData.SetEntityName(uid, GetTitle(target.Value, comp.Title), args.Meta);
    }

    /// <summary>
    /// Sets the Target field for the title and other components to use.
    /// </summary>
    public void SetTarget(string target, TargetDepartmentComponent comp)
    {
        comp.Title = target;
    }

    /// <summary>
    /// Gets the target from the component.
    /// </summary>
    /// <remarks>
    /// If it is null then the prototype is invalid, just return.
    /// </remarks>
    public bool GetTarget(EntityUid uid, [NotNullWhen(true)] out String? target, TargetDepartmentComponent? comp = null)
    {
        target = Resolve(uid, ref comp) ? comp.Title : null;
        return target != null;
    }
}
