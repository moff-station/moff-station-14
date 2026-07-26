namespace Content.Server._Moffstation.Objectives.Components;

/// <summary>
/// Marks the entity whose objectives are the source of truth for followers carrying
/// <see cref="MoffCommonObjectivesComponent"/>. Consumers (e.g. antag rules) query the people they
/// spawned for this component to find which one is the authority, then set it as each follower's
/// <see cref="MoffCommonObjectivesComponent.Authority"/>.
/// </summary>
[RegisterComponent]
public sealed partial class MoffSharedObjectiveAuthorityComponent : Component;
