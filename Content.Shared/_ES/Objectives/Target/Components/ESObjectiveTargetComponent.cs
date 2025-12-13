namespace Content.Shared._ES.Objectives.Target.Components;

/// <summary>
/// Marker component with <see cref="ESTargetObjectiveComponent"/>
/// </summary>
[RegisterComponent]
[Access(typeof(ESTargetObjectiveSystem), Other = AccessPermissions.None)]
public sealed partial class ESObjectiveTargetComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Objectives = new();
}
