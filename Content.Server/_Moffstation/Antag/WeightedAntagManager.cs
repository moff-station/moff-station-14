using Content.Server.Database;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;

namespace Content.Server._Moffstation.Antag;

public sealed class WeightedAntagManager
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ITaskManager _task = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
}
