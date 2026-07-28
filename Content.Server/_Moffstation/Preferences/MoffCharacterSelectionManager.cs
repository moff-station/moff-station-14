using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.Preferences.Managers;
using Content.Shared._Moffstation.Preferences;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Preferences;

/// <summary>
/// Authoritative store for player-global job priorities and which character slots are active.
/// Hooks its own callbacks into <see cref="UserDbDataManager"/> so ServerPreferencesManager needs
/// no modification.
/// </summary>
public sealed class MoffCharacterSelectionManager : IPostInjectInit
{
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private readonly Dictionary<NetUserId, MoffCharacterSelectionState> _cached = new();

    private ISawmill _sawmill = default!;

    public void Init()
    {
        _sawmill = _log.GetSawmill("moff.charselect");

        _netManager.RegisterNetMessage<MsgMoffCharacterSelectionState>();
        _netManager.RegisterNetMessage<MsgUpdateMoffJobPriorities>(HandleUpdateJobPriorities);
        _netManager.RegisterNetMessage<MsgSetMoffCharacterEnabled>(HandleSetCharacterEnabled);
    }

    public bool TryGetState(NetUserId userId, [NotNullWhen(true)] out MoffCharacterSelectionState? state)
    {
        return _cached.TryGetValue(userId, out state);
    }

    /// <summary>
    /// Returns a throwaway default if nothing is cached, so do not mutate the result.
    /// </summary>
    public MoffCharacterSelectionState GetState(NetUserId userId)
    {
        return _cached.TryGetValue(userId, out var state) ? state : new MoffCharacterSelectionState();
    }

    public JobPriority GetPriority(NetUserId userId, ProtoId<JobPrototype> job)
    {
        return GetState(userId).GetPriority(job);
    }

    public bool IsSlotEnabled(NetUserId userId, int slot)
    {
        return GetState(userId).IsSlotEnabled(slot);
    }

    /// <summary>
    /// Falls back to the per-character priority for guests and not-yet-loaded players, who would
    /// otherwise be eligible for no jobs at all.
    /// </summary>
    public JobPriority GetEffectivePriority(
        NetUserId userId,
        ProtoId<JobPrototype> job,
        HumanoidCharacterProfile fallback)
    {
        if (TryGetState(userId, out var state) && state.IsAuthoritative)
            return state.GetPriority(job);

        return fallback.JobPriorities.GetValueOrDefault(job, JobPriority.Never);
    }

    /// <summary>
    /// For tests and admin tooling; ordinary changes arrive as <see cref="MsgUpdateMoffJobPriorities"/>.
    /// </summary>
    public async Task SetJobPriorities(NetUserId userId, Dictionary<ProtoId<JobPrototype>, JobPriority> priorities)
    {
        if (!_cached.TryGetValue(userId, out var state))
            return;

        state.JobPriorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>(priorities);
        state.Normalize();

        if (!state.IsAuthoritative)
            return;

        await _db.SaveMoffJobPriorities(userId, state.JobPriorities);
    }

    #region Lifecycle

    // Should only be called via UserDbDataManager.
    private async Task LoadData(ICommonSession session, CancellationToken cancel)
    {
        // Guests get a transient default rather than a database row.
        if (!ShouldStore(session))
        {
            _cached[session.UserId] = new MoffCharacterSelectionState();
            return;
        }

        var state = await _db.GetMoffCharacterSelection(session.UserId, cancel);

        cancel.ThrowIfCancellationRequested();

        _cached[session.UserId] = state;
    }

    private void FinishLoad(ICommonSession session)
    {
        SendState(session);
    }

    private void OnClientDisconnected(ICommonSession session)
    {
        _cached.Remove(session.UserId);
    }

    private void SendState(ICommonSession session)
    {
        if (!_cached.TryGetValue(session.UserId, out var state))
            return;

        _netManager.ServerSendMessage(new MsgMoffCharacterSelectionState { State = state }, session.Channel);
    }

    private static bool ShouldStore(ICommonSession session)
    {
        return ServerPreferencesManager.ShouldStorePrefs(session.Channel.AuthType);
    }

    #endregion

    #region Net message handlers

    private async void HandleUpdateJobPriorities(MsgUpdateMoffJobPriorities message)
    {
        var userId = message.MsgChannel.UserId;

        if (!_cached.TryGetValue(userId, out var state))
            return;

        var sanitized = new Dictionary<ProtoId<JobPrototype>, JobPriority>();

        foreach (var (job, priority) in message.JobPriorities)
        {
            // Drop anything the client made up.
            if (!_protoManager.HasIndex(job))
                continue;

            if (priority == JobPriority.Never)
                continue;

            sanitized[job] = priority;
        }

        state.JobPriorities = sanitized;
        state.Normalize();

        if (!ServerPreferencesManager.ShouldStorePrefs(message.MsgChannel.AuthType))
            return;

        try
        {
            await _db.SaveMoffJobPriorities(userId, state.JobPriorities);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to save job priorities for {userId}: {e}");
        }
    }

    private async void HandleSetCharacterEnabled(MsgSetMoffCharacterEnabled message)
    {
        var userId = message.MsgChannel.UserId;

        if (!_cached.TryGetValue(userId, out var state))
            return;

        state.EnabledSlots[message.Slot] = message.Enabled;

        _sawmill.Debug($"Set slot {message.Slot} enabled={message.Enabled} for {userId}");

        if (!ServerPreferencesManager.ShouldStorePrefs(message.MsgChannel.AuthType))
            return;

        try
        {
            await _db.SaveMoffCharacterEnabled(userId, message.Slot, message.Enabled);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to save character enabled state for {userId}: {e}");
        }
    }

    #endregion

    void IPostInjectInit.PostInject()
    {
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnFinishLoad(FinishLoad);
        _userDb.AddOnPlayerDisconnect(OnClientDisconnected);
    }
}
