using System.Threading;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.RoundEnd.Components;

/// <summary>
/// A round restart pushed back by an extension vote, carried out by <see cref="RoundEndSystem"/> once <see cref="RestartAt"/> passes.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(RoundEndSystem))]
public sealed partial class RoundEndExtensionRestartComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan RestartAt;

    public CancellationToken Token;
}
