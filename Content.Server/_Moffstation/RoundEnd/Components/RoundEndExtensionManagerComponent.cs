using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.RoundEnd.Components;

/// <summary>
/// Post-round extension vote state for the current round, driven by <see cref="RoundEndSystem"/>.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(RoundEndSystem))]
public sealed partial class RoundEndExtensionManagerComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? NextVote;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan RestartAt;

    /// <summary>
    /// Extension votes passed so far.
    /// </summary>
    [DataField]
    public int Extensions;
}
