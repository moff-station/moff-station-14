using System.Linq;
using Content.Server.NameIdentifier;
using Content.Shared.Examine;
using Content.Shared.Kitchen;
using Content.Shared.NameIdentifier;
using Content.Shared._Moffstation.Chitter;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.Chitter;

public sealed partial class ChitterAccountSystem : SharedChitterSystem
{
    [Dependency] private ChitterServerSystem _server = default!;
    [Dependency] private NameIdentifierSystem _nameIdentifier = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IRobustRandom _random = default!;

    private static readonly ProtoId<NameIdentifierGroupPrototype> ChitterGroup = "Chitter";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChitterAccountComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ChitterAccountComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChitterAccountComponent, BeingMicrowavedEvent>(OnMicrowaved);
        SubscribeLocalEvent<ChitterAccountComponent, ComponentShutdown>(OnAccountShutdown);
    }

    // Prunes the account so a destroyed/gibbed card doesn't linger as a ghost contact.
    private void OnAccountShutdown(Entity<ChitterAccountComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.AccountId == 0)
            return;

        var query = EntityQueryEnumerator<ChitterServerComponent>();
        while (query.MoveNext(out var server))
        {
            _server.RemoveAccount(server, ent.Comp.AccountId);
        }
    }

    private void OnMapInit(Entity<ChitterAccountComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.AccountId == 0)
        {
            _nameIdentifier.GenerateUniqueName(ent, ChitterGroup, out var number);
            SetAccountId(ent, (uint)number);
        }

        if (string.IsNullOrEmpty(ent.Comp.ProfilePictureId))
            AssignRandomProfilePicture(ent);
    }

    private void OnMicrowaved(Entity<ChitterAccountComponent> ent, ref BeingMicrowavedEvent args)
    {
        _nameIdentifier.GenerateUniqueName(ent, ChitterGroup, out var number);
        SetAccountId(ent, (uint)number);
    }

    private void AssignRandomProfilePicture(Entity<ChitterAccountComponent> ent)
    {
        var avatars = _prototypeManager.EnumeratePrototypes<ChitterAvatarPrototype>().ToList();
        if (avatars.Count == 0)
            return;

        var avatar = _random.Pick(avatars);
        SetProfilePictureId(ent, avatar.ID);
    }
}
