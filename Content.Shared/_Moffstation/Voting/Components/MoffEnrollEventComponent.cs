using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Moffstation.Voting.Components;

/// <summary>
/// This is used for...
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
    [DataField, AutoNetworkedField]
    public int MaxEnrolled;

    /// <summary>
    /// Whether you can select a character to use for the event
    /// </summary>
    [DataField]
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
    /// Whether the event is available to be enrolled in
    /// </summary>
    [ViewVariables]
    public bool Enrollable = true;

    /// <summary>
    /// Whether the event's spawn location exists yet, i.e. whether a ghost can warp to it. Set once the
    /// owning rule has been added, which loads its map and picks its antag spawn location.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool Warpable;

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
/// Sent by a ghost asking to be warped to where this event will spawn.
/// </summary>
[Serializable, NetSerializable]
public sealed class MoffEnrollGotoMessage(NetEntity enroller) : EntityEventArgs
{
    public NetEntity Enroller = enroller;
}
