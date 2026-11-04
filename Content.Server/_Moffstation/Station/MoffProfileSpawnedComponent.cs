using Content.Shared.Preferences;

namespace Content.Server._Moffstation.Station;

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
