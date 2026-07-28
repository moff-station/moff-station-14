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
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

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
        var state = GetState(userId);

        if (state.IsAuthoritative)
            return state.GetPriority(job);

        // The job may have come from an active character other than the caller's fallback, so take
        // the strongest priority any of them gives it rather than only asking the fallback.
        var best = fallback.JobPriorities.GetValueOrDefault(job, JobPriority.Never);

        if (!_prefs.TryGetCachedPreferences(userId, out var prefs))
            return best;

        foreach (var (slot, profile) in prefs.Characters)
        {
            if (profile is not HumanoidCharacterProfile humanoid || !state.IsSlotEnabled(slot))
                continue;

            var priority = humanoid.JobPriorities.GetValueOrDefault(job, JobPriority.Never);

            if (priority > best)
                best = priority;
        }

        return best;
    }

    /// <summary>
    /// For tests and admin tooling; ordinary changes arrive as <see cref="MsgUpdateMoffJobPriorities"/>.
    /// </summary>
    /// <remarks>
    /// Creates transient state rather than dropping the write when nothing is cached, so dummy
    /// sessions in tests -- which never run the database load -- still take the priorities. Only
    /// does so for a connected user, or the entry would linger for someone who has left.
    /// </remarks>
    public async Task SetJobPriorities(NetUserId userId, Dictionary<ProtoId<JobPrototype>, JobPriority> priorities)
    {
        if (!_cached.TryGetValue(userId, out var state))
        {
            if (!_playerManager.TryGetSessionById(userId, out var session))
                return;

            // A real player's state arrives from the database; standing in for it here would drop
            // the write silently and leave them non-authoritative for the rest of the session.
            if (ShouldStore(session))
            {
                _sawmill.Error($"Job priorities set for {userId} before their selection state loaded; discarding.");
                return;
            }

            state = new MoffCharacterSelectionState();
            _cached[userId] = state;
        }

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
        SendState(session.Channel);
    }

    private void SendState(INetChannel channel)
    {
        if (!_cached.TryGetValue(channel.UserId, out var state))
            return;

        // A copy, because the message is serialized after this returns and the cached instance may
        // be mutated in between by another message from the same client.
        _netManager.ServerSendMessage(
            new MsgMoffCharacterSelectionState { State = new MoffCharacterSelectionState(state) },
            channel);
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

        // The client applied its own copy optimistically, so it has to be told what we actually
        // stored -- sanitizing and Normalize may both have changed it, and a failed write below
        // would otherwise leave the two silently disagreeing until the player reconnects.
        var previous = state.JobPriorities;

        state.JobPriorities = sanitized;
        state.Normalize();

        if (!ServerPreferencesManager.ShouldStorePrefs(message.MsgChannel.AuthType))
        {
            SendState(message.MsgChannel);
            return;
        }

        try
        {
            await _db.SaveMoffJobPriorities(userId, state.JobPriorities);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to save job priorities for {userId}: {e}");
            state.JobPriorities = previous;
        }

        SendState(message.MsgChannel);
    }

    private async void HandleSetCharacterEnabled(MsgSetMoffCharacterEnabled message)
    {
        var userId = message.MsgChannel.UserId;

        if (!_cached.TryGetValue(userId, out var state))
            return;

        // As above: the client already toggled its own copy, so it needs the stored result back.
        var hadPrevious = state.EnabledSlots.TryGetValue(message.Slot, out var previous);

        state.EnabledSlots[message.Slot] = message.Enabled;

        _sawmill.Debug($"Set slot {message.Slot} enabled={message.Enabled} for {userId}");

        if (!ServerPreferencesManager.ShouldStorePrefs(message.MsgChannel.AuthType))
        {
            SendState(message.MsgChannel);
            return;
        }

        try
        {
            await _db.SaveMoffCharacterEnabled(userId, message.Slot, message.Enabled);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to save character enabled state for {userId}: {e}");

            if (hadPrevious)
                state.EnabledSlots[message.Slot] = previous;
            else
                state.EnabledSlots.Remove(message.Slot);
        }

        SendState(message.MsgChannel);
    }

    #endregion

    void IPostInjectInit.PostInject()
    {
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnFinishLoad(FinishLoad);
        _userDb.AddOnPlayerDisconnect(OnClientDisconnected);
    }
}
