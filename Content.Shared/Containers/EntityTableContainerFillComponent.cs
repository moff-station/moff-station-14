using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Shared.Containers;

/// <summary>
/// Version of <see cref="ContainerFillComponent"/> that utilizes <see cref="EntityTableSelector"/>
/// </summary>
[RegisterComponent, Access(typeof(ContainerFillSystem))]
public sealed partial class EntityTableContainerFillComponent : Component
{
    [DataField]
    public Dictionary<string, EntityTableSelector> Containers = new();

    // Moffstation - Begin - Allow EntityTableContainerFillComponent to fail gracefully
    /// If false, trying to overfill a container fill will emit an error.
    [DataField]
    public bool SkipFillsWhichDontFit = false;
    // Moffstation - End
}
