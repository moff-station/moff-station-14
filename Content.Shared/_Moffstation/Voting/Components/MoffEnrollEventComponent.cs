using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Moffstation.Voting.Components;

/// <summary>
/// A vote entry players enroll in to become an owning game rule's antag(s). When the timer runs out the
/// enrolled players get assigned; if fewer than <see cref="MinEnrolled"/> enrolled, <see cref="FallbackRules"/>
/// start instead. Resolved server-side in <c>MoffEnrollEventSystem</c>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class MoffEnrollEventComponent : Component
{
    /// <summary>
    /// When the vote will end.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// Total duration of vote.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The minimum number of people needed for the rule to be accepted at the end of the countdown
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MinEnrolled = 1;

    /// <summary>
    /// The max amount of roles for this rule
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public int MaxEnrolled;

    /// <summary>
    /// Whether you can pick a character for the event. Resolved at runtime from the antag: false when it
    /// spawns a fixed non-humanoid body, since the picked character never gets applied to one.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool CharacterSelection = true;

    /// <summary>
    /// Color of the vote's title. Resolved at runtime from the antag this vote hands out: its mind
    /// role's subtype color, falling back to that role type's color.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Color TitleColor = Color.White;

    [DataField]
    public Color DescriptionColor = Color.LightGray;

    [DataField, AutoNetworkedField]
    public HashSet<NetEntity> Enrolled = new();

    /// <summary>
    /// Enrollees who asked to spawn as a randomly generated character instead of their selected one.
    /// Purely a spawn-time preference; nothing is written to the player's saved characters.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<NetEntity> RandomPick = new();

    /// <summary>
    /// Whether the event is available to be enrolled in
    /// </summary>
    [ViewVariables]
    public bool Enrollable = true;

    /// <summary>
    /// Whether the spawn location exists yet, i.e. whether a ghost can warp to it. Set once the owning rule's
    /// been added, which loads its map and picks the antag spawn.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool Warpable;

    /// <summary>
    /// Vote-manager rule that spawned this enroll entity. Resolved once, server-side; null/non-null doubles
    /// as "has this been resolved yet" (spawn picked, title color and character selection derived). Not networked.
    /// </summary>
    [ViewVariables]
    public EntityUid? OwningRule;

    /// <summary>
    /// Game rule(s) to start instead of the antag rule if fewer than <see cref="MinEnrolled"/> players
    /// enrolled when the timer runs out. Evaluated as an entity table of game rule prototypes.
    /// </summary>
    [DataField]
    public EntityTableSelector? FallbackRules;
}

[Serializable, NetSerializable]
public sealed class MoffSetEnrollMessage(NetEntity enroller, bool enrolled) : EntityEventArgs
{
    public NetEntity Enroller = enroller;
    public bool Enrolled = enrolled;
}

/// <summary>
/// Sent by an enrollee toggling whether they want to spawn as a randomly generated character rather than
/// the one they have selected.
/// </summary>
[Serializable, NetSerializable]
public sealed class MoffSetEnrollRandomMessage(NetEntity enroller, bool random) : EntityEventArgs
{
    public NetEntity Enroller = enroller;
    public bool Random = random;
}

/// <summary>
/// Sent by a ghost asking to be warped to where this event will spawn.
/// </summary>
[Serializable, NetSerializable]
public sealed class MoffEnrollGotoMessage(NetEntity enroller) : EntityEventArgs
{
    public NetEntity Enroller = enroller;
}
