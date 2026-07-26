using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Objectives.Components;

/// <summary>
/// This is used for sharing objectives between entities
/// when the authority has their objectives updated the changes will be copied down.
/// By default, the authority will be an entity spawned by the same gamerule with the <see cref="MoffCommonObjectiveAuthorityComponent"/>
/// If there are potential multiple authorities within a single gamerule, or you are not using a gamerule, set the authority manually in C#
/// </summary>
[RegisterComponent]
public sealed partial class MoffCommonObjectivesComponent : Component
{
    /// <summary>
    /// What objective should be used as a placeholder
    /// </summary>
    [DataField]
    public EntProtoId PlaceholderProtoId = "MoffSharedObjectivesPlaceholder";

    /// <summary>
    /// What entity are objectives copied from
    /// </summary>
    [ViewVariables]
    public EntityUid? Authority;

    [ViewVariables]
    public EntityUid? PlaceHolder;
}
