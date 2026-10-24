using Content.Shared._Moffstation.Extensions;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server._Moffstation.Station;

/// Maintains <see cref="MoffProfileSpawnedComponent"/> on player-controlled entities.
public sealed partial class MoffProfileSpawnedSystem : EntitySystem
{
    [Dependency] private IPlayerManager _playerMan = default!;

    [Dependency] private EntityQuery<MoffProfileSpawnedComponent> _profileSpawnedQuery;

    [SubscribeLocalEvent]
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        EnsureComp<MoffProfileSpawnedComponent>(args.Mob).Profile = args.Profile;
    }

    /// Returns the <see cref="MoffProfileSpawnedComponent.IResult"/> describing the entity attached to
    /// <paramref name="player"/> (if it exists).
    public MoffProfileSpawnedComponent.IResult GetSpawnedProfile(NetUserId player) =>
        _playerMan.TryGetSessionById(player, out var session)
            ? new MoffProfileSpawnedComponent.IResult.AttachedEntity(
                _profileSpawnedQuery
                    .ResolveOrNull(session.AttachedEntity, logMissing: false)
                    ?.Comp.Profile
            )
            : new MoffProfileSpawnedComponent.IResult.NoAttachedEntity();
}

/// A component which tracks what <see cref="HumanoidCharacterProfile"/> was used to spawn this entity.
[RegisterComponent, Access(typeof(MoffProfileSpawnedSystem))]
public sealed partial class MoffProfileSpawnedComponent : Component
{
    /// The profile used to spawn the owner entity of this component.
    [DataField(required: true)]
    public HumanoidCharacterProfile Profile = new();

    /// The result of <see cref="MoffProfileSpawnedSystem.GetSpawnedProfile"/>.
    public interface IResult
    {
        /// The <see cref="HumanoidCharacterProfile"/> used to spawn an entity.
        HumanoidCharacterProfile? Profile { get; }

        /// An <see cref="IResult"/> indicating that the queried player does not have an entity spawned.
        public readonly partial record struct NoAttachedEntity : IResult
        {
            /// <inheritdoc/>
            /// Always null for this variant, as there is no entity spawned.
            public HumanoidCharacterProfile? Profile => null;
        }

        /// An <see cref="IResult"/> indicating that the queried player has an entity.
        public readonly partial record struct AttachedEntity(HumanoidCharacterProfile? Profile) : IResult;
    }
}
