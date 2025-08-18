using System.Linq;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
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
    [Dependency] private readonly PlayTimeTrackingSystem _playTimeTracking = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly Dictionary<ICommonSession, ReadyManifestEui> _openEuis = [];

    // A dictionary for each job type, then another for each priority level for that job type
    private Dictionary<ProtoId<JobPrototype>, (int High, int Medium, int Low)> _jobCounts = [];

    public override void Initialize()
    {
        SubscribeNetworkEvent<RequestReadyManifestMessage>(OnRequestReadyManifest);
        SubscribeLocalEvent<PlayerToggleReadyEvent>(OnPlayerToggleReady);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        foreach (var (_, eui) in _openEuis)
        {
            eui.Close();
        }

        _openEuis.Clear();
    }

    private void OnRequestReadyManifest(RequestReadyManifestMessage message, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { } sessionCast)
        {
            return;
        }
        BuildReadyManifest();
        OpenEui(sessionCast);
    }

    private void OnPlayerToggleReady(ref PlayerToggleReadyEvent ev)
    {
        UpdateByPlayer(ev.PlayerSession.Data.UserId);
        UpdateEuis();
    }

    private void BuildReadyManifest()
    {
        _jobCounts.Clear();

        foreach (var (userId, _) in _gameTicker.PlayerGameStatuses)
        {
            UpdateByPlayer(userId);
        }
    }

    private void UpdateByPlayer(NetUserId userId)
    {
        if (!_prefsManager.TryGetCachedPreferences(userId, out var preferences))
        {
            return;
        }

        var profile = (HumanoidCharacterProfile)preferences.SelectedCharacter;
        var player = _playerManager.GetSessionById(userId);
        var profileJobs = FilterPlayerJobs(profile, player);

        var isReady = _gameTicker.PlayerGameStatuses[userId] == PlayerGameStatus.ReadyToPlay;

        foreach (var job in profileJobs)
        {
            if (!_jobCounts.TryGetValue(job, out var counts) ||
                !profile.JobPriorities.TryGetValue(job, out var priority))
            {
                continue;
            }

            if (priority is not (JobPriority.High or JobPriority.Medium or JobPriority.Low))
                continue;

            int delta = isReady ? 1 : -1;

            _jobCounts[job] = priority switch
            {
                JobPriority.High => counts with { High = counts.High + delta },
                JobPriority.Medium => counts with { Medium = counts.Medium + delta },
                JobPriority.Low => counts with { Low = counts.Low + delta },
                _ => counts
            };
        }
    }

    private List<ProtoId<JobPrototype>> FilterPlayerJobs(HumanoidCharacterProfile profile, ICommonSession player)
    {
        var jobs = profile.JobPriorities.Keys.Select(k => new ProtoId<JobPrototype>(k)).ToList();
        List<ProtoId<JobPrototype>> priorityJobs = [];
        foreach (var job in jobs)
        {
            var priority = profile.JobPriorities[job];
            // For jobs that are rolled before others such as Command, we want to check for any priority since they'll always be filled
            if ((priority == JobPriority.High ||
                 _prototypeManager.Index(job).Weight > 0 &&
                 priority > JobPriority.Never) &&
                _playTimeTracking.IsAllowed(player, job))
            {
                priorityJobs.Add(job);
            }
        }
        return priorityJobs;
    }

    public Dictionary<ProtoId<JobPrototype>, (int High, int Medium, int Low)> GetReadyManifest()
    {
        return _jobCounts;
    }

    public void OpenEui(ICommonSession session)
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
        foreach (var (_, eui) in _openEuis)
        {
            eui.StateDirty();
        }
    }

    public void CloseEui(ICommonSession session)
    {
        if (!_openEuis.TryGetValue(session, out var eui))
        {
            return;
        }

        _openEuis.Remove(session);
        eui.Close();
    }
}
