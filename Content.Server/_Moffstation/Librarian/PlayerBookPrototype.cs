using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Moffstation.Librarian;

/// <summary>
/// This is a prototype for storing player made books in the database
/// </summary>
[Prototype]
public sealed partial class PlayerBookPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = default!;

    [DataField]
    public string Description = default!;

    [DataField]
    public string Author = default!;

    [DataField]
    public string Content = default!;

    [DataField]
    public SpriteSpecifier Icon = default!;
}
