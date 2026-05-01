using Robust.Shared.GameStates;
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
    /// Whether you can select a character to use for the event
    /// </summary>
    [DataField]
    public bool CharacterSelection = true;

    /// <summary>
    /// Whether the event is available to be enrolled in
    /// </summary>
    [ViewVariables]
    public bool Enrollable = true;
}
