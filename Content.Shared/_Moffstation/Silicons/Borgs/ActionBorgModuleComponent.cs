using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Silicons.Borgs;

/// <summary>
/// This is used for a <see cref="BorgModuleComponent"/> that provide actions to the entity it's installed into.
/// </summary>
/// <remarks>
/// Attempting to do this by adding an <see cref="ActionGrantComponent"/> via a <see cref="ComponentBorgModuleComponent"/>
/// will override any ActionGrantComponent already present in the entity.
/// </remarks>
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class ActionBorgModuleComponent : Component
{
    /// <summary>
    /// What actions should be granted once this module is installed into a borg chassis.
    /// </summary>
    [DataField(required: true), AutoNetworkedField, AlwaysPushInheritance]
    public List<EntProtoId> Actions = new();

    [DataField, AutoNetworkedField]
    public List<EntityUid> ActionEntities = new();
}
