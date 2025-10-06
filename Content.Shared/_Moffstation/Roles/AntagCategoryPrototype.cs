using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Roles;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype()]
public sealed partial class AntagCategoryPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// The name LocId of the antag catagory that will be displayed in the various menus.
    /// </summary>
    [DataField(required: true)]
    public LocId Name = string.Empty;

    /// <summary>
    /// A description LocId to display in the character menu as an explanation of the faction
    /// </summary>
    [DataField(required: true)]
    public LocId Description = string.Empty;

    /// <summary>
    /// A color representing this department to use for text.
    /// </summary>
    [DataField(required: true)]
    public Color Color;

    [DataField]
    public Color TextColor =  Color.White;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<ProtoId<AntagPrototype>> Roles = new();

    /// <summary>
    /// Departments with a higher weight sorted before other departments in UI.
    /// </summary>
    [DataField]
    public int Weight { get; private set; }

    /// <summary>
    /// Toggles the display of the department in the priority setting menu in the character editor.
    /// </summary>
    [DataField]
    public bool EditorHidden;

    /// <summary>
    /// Antags which belong to no category will be assigned to this one automatically
    /// </summary>
    [DataField]
    public bool Default;
}
