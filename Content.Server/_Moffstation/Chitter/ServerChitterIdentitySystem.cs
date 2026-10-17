using Content.Server.NameIdentifier;
using Content.Shared._Moffstation.Chitter;
using Content.Shared.NameIdentifier;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Chitter;

public sealed partial class ServerChitterIdentitySystem : ChitterIdentitySystem
{
    [Dependency] private NameIdentifierSystem _nameIdentifier = default!;

    private static readonly ProtoId<NameIdentifierGroupPrototype> ChitterGroup = "Chitter";

    protected override ChitterAccountId GetNewId()
    {
        _nameIdentifier.GenerateUniqueNameModifier(ChitterGroup, out var number);
        return new ChitterAccountId(number);
    }
}
