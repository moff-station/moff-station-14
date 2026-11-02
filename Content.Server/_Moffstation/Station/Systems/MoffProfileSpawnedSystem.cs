using Content.Shared._Moffstation.Extensions;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server._Moffstation.Station;

/// Maintains <see cref="MoffProfileSpawnedComponent"/> on player-controlled entities.
public sealed partial class MoffProfileSpawnedSystem : EntitySystem
{
    [Dependency] private IPlayerManager _playerMan = default!;

    [Dependency] private EntityQuery<MoffProfileSpawnedComponent> _profileSpawnedQuery;

    [SubscribeLocalEvent]
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args) => SetSpawnedProfile(args.Mob, args.Profile);

    /// Returns the <see cref="MoffProfileSpawnedComponent.IResult"/> describing the entity attached to
    /// <paramref name="player"/> (if it exists).
    [PublicAPI]
    public MoffProfileSpawnedComponent.IResult GetSpawnedProfile(NetUserId player) =>
        _playerMan.TryGetSessionById(player, out var session)
            ? new MoffProfileSpawnedComponent.IResult.AttachedEntity(
                _profileSpawnedQuery.ResolveOrNull(session.AttachedEntity, logMissing: false)
                    ?.Comp.Profile
            )
            : new MoffProfileSpawnedComponent.IResult.NoAttachedEntity();

    /// Sets the <see cref="MoffProfileSpawnedComponent"/> on <paramref name="mob"/> to contain <paramref name="profile"/>.
    /// If <paramref name="profile"/> is null, removes the component.
    /// Whatever is assigned here can be expected to be returned by <see cref="GetSpawnedProfile"/>.
    [PublicAPI]
    public void SetSpawnedProfile(EntityUid mob, HumanoidCharacterProfile? profile)
    {
        if (profile is null)
        {
            RemComp<MoffProfileSpawnedComponent>(mob);
            return;
        }

        if (EnsureComp<MoffProfileSpawnedComponent>(mob, out var comp))
        {
            this.AssertOrLogError(
                $"{nameof(MoffProfileSpawnedComponent)} already present on {ToPrettyString(mob)}");
        }

        comp.Profile = profile;
    }
}
