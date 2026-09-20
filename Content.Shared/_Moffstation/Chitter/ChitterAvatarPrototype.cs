using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Chitter;

[Prototype]
public sealed partial class ChitterAvatarPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    // Excluded from the profile picture picker - used only for the fake identities a Chitter server
    // hands out to everyone once it's been emagged (see ChitterServerSystem.ApplyEmagDisguise).
    [DataField]
    public bool Hidden;
}
