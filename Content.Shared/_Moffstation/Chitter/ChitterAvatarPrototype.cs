using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Chitter;

/// The definition of an available chitter user avatar.
[Prototype]
public sealed partial class ChitterAvatarPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// The actual sprite of the avatar.
    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    /// If true, this avatar isn't selectable from the normal user UI. It is handed out by some other mechanism, eg.
    /// emagging the server.
    [DataField]
    public bool NotSelectable;
}
