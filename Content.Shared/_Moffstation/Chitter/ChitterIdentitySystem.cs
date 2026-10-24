using System.Linq;
using Content.Shared._Moffstation.PDA;
using Content.Shared.Access.Systems;
using Content.Shared.Cloning.Events;
using Content.Shared.Examine;
using Content.Shared.Kitchen;
using Content.Shared.Random.Helpers;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Chitter;

/// This system manages <see cref="ChitterIdentityComponent"/>.
public abstract partial class ChitterIdentitySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    /// The avatar to use whenever we need an avatar but a chitter account doesn't have one for some reason.
    public static readonly ProtoId<ChitterAvatarPrototype> DefaultAvatar = "ChitterAvatar21";

    /// Gets the <see cref="ChitterAccount"/> data for the given <paramref name="entity"/>.
    /// <seealso cref="ChitterGetIdentityInfoEvent"/>
    [PublicAPI] public ChitterAccount? GetAccountOrNull(Entity<ChitterIdentityComponent> entity)
    {
        var ev = new ChitterGetIdentityInfoEvent();
        RaiseLocalEvent(entity, ref ev);

        return ev is { Name: { } name, Title: { } title }
            ? new ChitterAccount(entity.Comp.AccountId, name, title, entity.Comp.Avatar ?? DefaultAvatar)
            : null;
    }

    /// Sets <paramref name="entity"/>'s avatar.
    [PublicAPI] public void SetAvatar(Entity<ChitterIdentityComponent> entity, ProtoId<ChitterAvatarPrototype> avatar)
    {
        entity.Comp.Avatar = avatar;
        Dirty(entity);
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<ChitterIdentityComponent> ent, ref ExaminedEvent args) =>
        args.PushMarkup(Loc.GetString("chitter-account-examine", ("number", ent.Comp.AccountId.Display())));

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<ChitterIdentityComponent> entity, ref MapInitEvent args)
    {
        entity.Comp.AccountId = GetNewId();
        entity.Comp.Avatar ??= GetRandomAvatar(entity);
        Dirty(entity);
    }

    [SubscribeLocalEvent]
    private void OnMicrowaved(Entity<ChitterIdentityComponent> entity, ref BeingMicrowavedEvent args)
    {
        entity.Comp.AccountId = GetNewId();
        entity.Comp.Avatar = GetRandomAvatar(entity);
        Dirty(entity);
    }

    [SubscribeLocalEvent(before: [typeof(SharedAgentIdCardSystem)])]
    private void OnNumberChanged(Entity<ChitterIdentityComponent> entity, ref AgentIDCardNumberChangedMessage args)
    {
        entity.Comp.AccountId = args.Id;
        Dirty(entity);
    }

    /// Gets a new unique Chitter account ID to provision to a new account. Can only be implemented on the server.
    protected abstract ChitterAccountId GetNewId();

    private ProtoId<ChitterAvatarPrototype>? GetRandomAvatar(Entity<ChitterIdentityComponent> entity)
    {
        var avatars = ProtoMan.EnumeratePrototypes<ChitterAvatarPrototype>()
            .Where(a => !a.NotSelectable)
            .ToList();
        if (avatars.Count == 0)
            return DefaultAvatar;

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(entity));
        return rand.Pick(avatars);
    }
}
