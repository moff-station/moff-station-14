using System.Linq;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared._Moffstation.ReadyManifest;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.ReadyManifest;

public sealed class ReadyManifestSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;


    private readonly Dictionary<ICommonSession, ReadyManifestEui> _openEuis = [];

    // A dictionary for each job type, then another for each priority level for that job type
    private readonly Dictionary<ProtoId<JobPrototype>, int> _jobCounts = [];

    public override void Initialize()
    {
        SubscribeNetworkEvent<RequestReadyManifestMessage>(OnRequestReadyManifest);
        SubscribeLocalEvent<PlayerToggleReadyEvent>(OnPlayerToggleReady);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        foreach (var eui in _openEuis.Values)
        {
            eui.Close();
        }

        _openEuis.Clear();
    }

    private void OnRequestReadyManifest(RequestReadyManifestMessage message, EntitySessionEventArgs args)
    {
        BuildReadyManifest();
        OpenEui(args.SenderSession);
    }

    private void OnPlayerToggleReady(ref PlayerToggleReadyEvent ev)
    {
        BuildReadyManifest();
        UpdateEuis();
    }

    private void BuildReadyManifest()
    {
        _jobCounts.Clear();

        var jobs = _protoMan.EnumeratePrototypes<JobPrototype>();
        foreach (var job in jobs)
        {
            if (!job.SetPreference)
                continue;
            _jobCounts.Add(job.ID, 0);
        }
        foreach (var userId in _gameTicker.PlayerGameStatuses.Keys)
        {
            UpdateByPlayer(userId);
        }
    }

    private void UpdateByPlayer(NetUserId userId)
    {
        // If they aren't ready, then don't bother counting them
        if (_gameTicker.PlayerGameStatuses[userId] != PlayerGameStatus.ReadyToPlay)
            return;

        if (!_prefsManager.TryGetCachedPreferences(userId, out var preferences))
            return;

        var profile = (HumanoidCharacterProfile)preferences.SelectedCharacter;
        var jobs = profile.JobPriorities.Keys.ToList();

        foreach (var job in jobs)
        {
            if (!_jobCounts.ContainsKey(job) ||
                !profile.JobPriorities.TryGetValue(job, out var priority))
                continue;

            if (priority < JobPriority.Medium)
                continue;

            _jobCounts[job]++;
        }
    }

    public IDictionary<ProtoId<JobPrototype>, int> GetReadyManifest()
    {
        return _jobCounts.AsReadOnly();
    }

    private void OpenEui(ICommonSession session)
    {
        if (_openEuis.ContainsKey(session))
        {
            return;
        }

        var eui = new ReadyManifestEui(this);
        _openEuis.Add(session, eui);
        _euiManager.OpenEui(eui, session);
        eui.StateDirty();
    }

    private void UpdateEuis()
    {
        foreach (var eui in _openEuis.Values)
        {
            eui.StateDirty();
        }
    }

    public void CloseEui(ICommonSession session)
    {
        if (_openEuis.Remove(session, out var eui))
            eui.Close();
    }
}
