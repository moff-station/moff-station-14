using Content.Server._Moffstation.Objectives.Systems;
using Content.Server.Objectives.Systems;

namespace Content.Server._Moffstation.Objectives.Components;

[RegisterComponent]
public sealed partial class StraightObjectiveComponent : Component
{
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string Title = string.Empty;
}
