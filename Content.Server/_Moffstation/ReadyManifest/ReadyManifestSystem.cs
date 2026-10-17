using Content.Server._Moffstation.Preferences;
using Content.Server._Moffstation.Station;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Station.Systems;
using Content.Shared._Moffstation.ReadyManifest;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.ReadyManifest;

public sealed partial class ReadyManifestSystem : EntitySystem
{
    [Dependency] private EuiManager _euiManager = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private MoffCharacterSelectionManager _selection = default!;
    [Dependency] private MoffJobCandidateSystem _candidates = default!;


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

        // A character only records whether it will take a job, so the priority has to come from the
        // player-global state, and every active character contributes its jobs.
        var counted = new HashSet<ProtoId<JobPrototype>>();

        foreach (var profile in _candidates.GetActiveProfiles(userId))
        {
            foreach (var job in profile.JobPriorities.Keys)
            {
                if (!_jobCounts.ContainsKey(job) || !counted.Add(job))
                    continue;

                // GetEffectivePriority, not GetPriority: a guest or a player whose database load
                // has not finished has an empty priority dictionary, and would count as Never.
                if (_selection.GetEffectivePriority(userId, job, profile) < JobPriority.High)
                    continue;

                _jobCounts[job]++;
            }
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
