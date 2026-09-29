using System.Threading;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.RoundEnd.Components;

/// <summary>
/// A pending round end extension vote, started by <see cref="RoundEndSystem"/> once <see cref="StartAt"/> passes.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(RoundEndSystem))]
public sealed partial class RoundEndExtensionVoteComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan StartAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan RestartTime;

    [DataField]
    public TimeSpan Duration;

    [DataField]
    public int Extensions;

    /// <summary>
    /// The round end countdown token this vote belongs to; the vote is dropped if it gets cancelled.
    /// </summary>
    public CancellationToken Token;
}
