using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Objectives.Components;

[RegisterComponent]
public sealed partial class DepartmentObjectiveComponent : Component
{

    [DataField(required: true)]
    public string Title = string.Empty;

    [DataField]
    public ProtoId<DepartmentPrototype>? Target;
}
