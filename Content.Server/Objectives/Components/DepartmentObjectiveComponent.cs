using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

[RegisterComponent, Access(typeof(TargetDepartmentSystem))]
public sealed partial class TargetDepartmentComponent : Component
{
    /// <summary>
    /// Locale id for the objective title.
    /// It is passed "targetName" and "job" arguments.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string Title = string.Empty;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Target;
}
