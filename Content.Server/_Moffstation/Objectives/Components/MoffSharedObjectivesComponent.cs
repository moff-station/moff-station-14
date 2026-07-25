using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Objectives.Components;

/// <summary>
/// This is used for sharing objectives between entities
/// when the authority has their objectives updated the changes will be copied down
/// </summary>
[RegisterComponent]
public sealed partial class MoffSharedObjectivesComponent : Component
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
