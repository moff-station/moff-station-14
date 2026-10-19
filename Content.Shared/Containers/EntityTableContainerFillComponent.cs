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

    /// <summary>
    /// Whether to sort the contents of the table by size before inserting.
    /// Helps with fitting items into containers.
    /// </summary>
    [DataField]
    public bool Sort;

    // Moffstation - Begin - Allow EntityTableContainerFillComponent to fail gracefully
    /// If false, trying to overfill a container fill will emit an error.
    /// Ideally this'd be a flag per container in <see cref="Containers"/>, but that is a much larger blast radius that
    /// I don't want to deal with when making a kinda hackfix to upstream code.
    [DataField]
    public bool SkipFillsWhichDontFit = false;
    // Moffstation - End
}
