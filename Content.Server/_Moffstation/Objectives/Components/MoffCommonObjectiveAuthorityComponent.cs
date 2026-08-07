namespace Content.Server._Moffstation.Objectives.Components;

/// <summary>
/// Entities with <see cref="MoffCommonObjectivesComponent"/> will chose entities with this component as their authority
/// on objectives, given they are part of the same rule. If there are potentially multiple things that can be the
/// "authority" within a single rule, or you are not using a gamerule, set it via C# instead of this component.
/// </summary>
[RegisterComponent]
public sealed partial class MoffCommonObjectiveAuthorityComponent : Component;
