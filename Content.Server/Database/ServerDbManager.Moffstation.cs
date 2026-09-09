using System.Threading;
using System.Threading.Tasks;
using Content.Shared._Moffstation.Preferences;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Database;

public sealed partial class ServerDbManager
{
    public Task<MoffCharacterSelectionState> GetMoffCharacterSelection(NetUserId userId, CancellationToken cancel = default)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetMoffCharacterSelectionAsync(userId, cancel));
    }

    public Task SaveMoffJobPriorities(NetUserId userId, Dictionary<ProtoId<JobPrototype>, JobPriority> priorities)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SaveMoffJobPrioritiesAsync(userId, priorities));
    }

    public Task SaveMoffCharacterEnabled(NetUserId userId, int slot, bool enabled)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SaveMoffCharacterEnabledAsync(userId, slot, enabled));
    }
}
