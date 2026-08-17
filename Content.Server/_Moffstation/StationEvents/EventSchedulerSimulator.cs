using System.Linq;
using System.Text;
using Content.Server._Moffstation.StationEvents.Components;
using Content.Server.StationEvents;
using Content.Server.StationEvents.Components;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.StationEvents;

/// <summary>
/// Runs a <see cref="MoffStationEventSchedulerComponent"/> over many simulated rounds to report how long it spends
/// in each state and which events it actually runs there. Used by the integration tests to tune scheduler timings.
/// </summary>
public sealed class EventSchedulerSimulator
{
    /// <summary>
    /// Safety net against a state configured with a zero duration or timing, which would never advance the clock.
    /// </summary>
    private const int MaxStepsPerRound = 10000;

    private static readonly TimeSpan MinEventDelay = TimeSpan.FromSeconds(1);

    private readonly IPrototypeManager _prototype;
    private readonly IComponentFactory _componentFactory;
    private readonly IRobustRandom _random;
    private readonly EventManagerSystem _eventManager;
    private readonly EntityTableSystem _entityTable;
    private readonly MoffStationEventSchedulerSystem _scheduler;

    public EventSchedulerSimulator(
        IPrototypeManager prototype,
        IComponentFactory componentFactory,
        IRobustRandom random,
        EventManagerSystem eventManager,
        EntityTableSystem entityTable,
        MoffStationEventSchedulerSystem scheduler)
    {
        _prototype = prototype;
        _componentFactory = componentFactory;
        _random = random;
        _eventManager = eventManager;
        _entityTable = entityTable;
        _scheduler = scheduler;
    }

    /// <summary>
    /// Simulates <paramref name="rounds"/> rounds of the given scheduler prototype.
    /// </summary>
    /// <param name="schedulerProto">A gamerule prototype with a <see cref="MoffStationEventSchedulerComponent"/>.</param>
    /// <param name="rounds">How many rounds to simulate.</param>
    /// <param name="playerCount">Player count to check event restrictions against.</param>
    /// <param name="roundLengthMean">Average round length, in minutes.</param>
    /// <param name="roundLengthStdDev">Standard deviation of the round length, in minutes.</param>
    public SimulationResult Simulate(
        EntProtoId schedulerProto,
        int rounds,
        int playerCount,
        float roundLengthMean,
        float roundLengthStdDev)
    {
        var proto = _prototype.Index(schedulerProto);

        if (!proto.TryComp<MoffStationEventSchedulerComponent>(out var scheduler, _componentFactory))
            throw new ArgumentException($"{schedulerProto} has no {nameof(MoffStationEventSchedulerComponent)}!");

        var result = new SimulationResult(schedulerProto, rounds, playerCount, roundLengthMean, roundLengthStdDev);

        foreach (var (stateId, state) in scheduler.States)
        {
            result.States[stateId] = new StateStats(state.EventTiming != null);
        }

        foreach (var (spawn, _) in ListPossibleEvents(scheduler))
        {
            result.PossibleEvents.Add(spawn.Id);
        }

        for (var i = 0; i < rounds; i++)
        {
            var roundLength = TimeSpan.FromMinutes(_random.NextGaussian(roundLengthMean, roundLengthStdDev));
            if (roundLength <= TimeSpan.Zero)
                continue;

            result.SimulatedRounds++;
            SimulateRound(scheduler, roundLength, playerCount, result);
        }

        return result;
    }

    private void SimulateRound(
        MoffStationEventSchedulerComponent scheduler,
        TimeSpan roundLength,
        int playerCount,
        SimulationResult result)
    {
        if (!scheduler.States.TryGetValue(scheduler.InitialState, out var state))
            throw new ArgumentException($"Scheduler has no state named \"{scheduler.InitialState}\" to start in!");

        var round = new RoundState();
        var stateId = scheduler.InitialState;
        var stateStart = TimeSpan.Zero;
        result.States[stateId].Visits++;

        var (stateEnd, _) = _scheduler.RollState(state);
        // The state's own timing doesn't apply to the very first event, matching the system.
        var nextEvent = TimeSpan.FromSeconds(scheduler.InitialDelay.Next(_random));

        // Zero-length durations or timings would otherwise never advance the clock.
        for (var step = 0; step < MaxStepsPerRound; step++)
        {
            var eventAt = state.EventTiming != null ? nextEvent : (TimeSpan?)null;
            if (Earliest(stateEnd, eventAt) is not { } time || time > roundLength)
                break;

            // State changes win ties, since the system checks them first.
            if (stateEnd is { } end && time >= end)
            {
                result.States[stateId].Time += time - stateStart;
                stateStart = time;

                if (_scheduler.PickNextState(state) is { } nextId &&
                    scheduler.States.TryGetValue(nextId, out var nextState))
                {
                    stateId = nextId;
                    state = nextState;
                    result.States[stateId].Visits++;

                    var (nextDuration, eventDelay) = _scheduler.RollState(state);
                    stateEnd = nextDuration is { } nextEnd ? time + nextEnd : null;
                    nextEvent = time + eventDelay;

                    if (state.EventOnEnter)
                        RunEvent(scheduler, state, stateId, time, playerCount, round, result);
                }
                else
                {
                    // Nowhere to go, so stay put for the rest of the round.
                    stateEnd = null;
                }

                continue;
            }

            RunEvent(scheduler, state, stateId, time, playerCount, round, result);

            var delay = TimeSpan.FromSeconds(state.EventTiming!.Value.Next(_random));
            nextEvent = time + (delay < MinEventDelay ? MinEventDelay : delay);
        }

        result.States[stateId].Time += roundLength - stateStart;

        foreach (var eventId in round.Occurrences.Keys)
        {
            result.RoundsWithEvent[eventId] = result.RoundsWithEvent.GetValueOrDefault(eventId) + 1;
        }
    }

    private void RunEvent(
        MoffStationEventSchedulerComponent scheduler,
        EventSchedulerState state,
        string stateId,
        TimeSpan currentTime,
        int playerCount,
        RoundState round,
        SimulationResult result)
    {
        var table = state.ScheduledGameRules ?? scheduler.ScheduledGameRules;

        if (!_eventManager.TryBuildLimitedEvents(table, out var candidates, currentTime, playerCount))
            return;

        // CanRun reads occurrences off the GameTicker, which knows nothing about a simulated round, so redo those
        // checks against our own bookkeeping.
        var valid = new Dictionary<EntityPrototype, StationEventComponent>();
        foreach (var (proto, stationEvent) in candidates)
        {
            if (stationEvent.MaxOccurrences is { } max && round.Occurrences.GetValueOrDefault(proto.ID) >= max)
                continue;

            if (round.LastRun.TryGetValue(proto.ID, out var lastRun) &&
                currentTime.TotalMinutes < lastRun + stationEvent.ReoccurrenceDelay)
            {
                continue;
            }

            valid.Add(proto, stationEvent);
        }

        if (valid.Count == 0 || _eventManager.FindEvent(valid) is not { } picked)
            return;

        round.Occurrences[picked] = round.Occurrences.GetValueOrDefault(picked) + 1;
        round.LastRun[picked] = currentTime.TotalMinutes;

        var stats = result.States[stateId];
        stats.Events++;
        stats.EventCounts[picked] = stats.EventCounts.GetValueOrDefault(picked) + 1;
    }

    /// <summary>
    /// Every event the scheduler could ever pick, ignoring restrictions, so unreachable entries can be reported.
    /// </summary>
    private IEnumerable<(EntProtoId, float)> ListPossibleEvents(MoffStationEventSchedulerComponent scheduler)
    {
        var tables = new List<EntityTableSelector> { scheduler.ScheduledGameRules };
        tables.AddRange(scheduler.States.Values
            .Select(state => state.ScheduledGameRules)
            .OfType<EntityTableSelector>());

        return tables.SelectMany(table => _entityTable.ListSpawns(table));
    }

    private static TimeSpan? Earliest(TimeSpan? a, TimeSpan? b)
    {
        if (a == null)
            return b;

        return b == null || a.Value < b.Value ? a : b;
    }

    /// <summary>
    /// Per-round event bookkeeping, standing in for what the GameTicker would know during a real round.
    /// </summary>
    private sealed class RoundState
    {
        public readonly Dictionary<string, int> Occurrences = new();
        public readonly Dictionary<string, double> LastRun = new();
    }
}

/// <summary>
/// What a scheduler did in one state, summed over every simulated round.
/// </summary>
public sealed class StateStats(bool firesEvents)
{
    /// <summary>
    /// Whether the state has any event timing at all, so the report can tell dead air apart from bad luck.
    /// </summary>
    public readonly bool FiresEvents = firesEvents;

    public int Visits;
    public TimeSpan Time;
    public int Events;
    public readonly Dictionary<string, int> EventCounts = new();
}

/// <summary>
/// The result of an <see cref="EventSchedulerSimulator"/> run, and the report built from it.
/// </summary>
public sealed class SimulationResult(
    EntProtoId scheduler,
    int requestedRounds,
    int playerCount,
    float roundLengthMean,
    float roundLengthStdDev)
{
    public readonly EntProtoId Scheduler = scheduler;
    public readonly int RequestedRounds = requestedRounds;
    public readonly int PlayerCount = playerCount;
    public readonly float RoundLengthMean = roundLengthMean;
    public readonly float RoundLengthStdDev = roundLengthStdDev;

    /// <summary>
    /// Rounds which were actually simulated, in case a round length rolled negative.
    /// </summary>
    public int SimulatedRounds;

    public readonly Dictionary<string, StateStats> States = new();

    /// <summary>
    /// Events which ran at least once in a given round, keyed by event id.
    /// </summary>
    public readonly Dictionary<string, int> RoundsWithEvent = new();

    /// <summary>
    /// Every event the scheduler's tables can produce, whether or not it ever ran.
    /// </summary>
    public readonly HashSet<string> PossibleEvents = new();

    public TimeSpan TotalTime => States.Values.Aggregate(TimeSpan.Zero, (total, state) => total + state.Time);

    public float EventsPerRound => SimulatedRounds == 0
        ? 0f
        : States.Values.Sum(state => state.Events) / (float)SimulatedRounds;

    /// <summary>
    /// Events which never ran once, usually meaning an unreachable table entry or a player count restriction.
    /// </summary>
    public IEnumerable<string> NeverFired => PossibleEvents.Where(id => !RoundsWithEvent.ContainsKey(id)).Order();

    /// <summary>
    /// Renders the whole run as a plaintext report.
    /// </summary>
    public string FormatTable()
    {
        var rounds = Math.Max(SimulatedRounds, 1);
        var stateIds = States.Keys.ToList();
        var builder = new StringBuilder();

        builder.AppendLine();
        builder.AppendLine($"=== {Scheduler} | {SimulatedRounds}/{RequestedRounds} rounds, " +
                           $"{RoundLengthMean:F1}±{RoundLengthStdDev:F1} min, {PlayerCount} players ===");
        builder.AppendLine();
        builder.AppendLine($"{"State",-16}{"Visits/rd",12}{"Time/rd",12}{"% round",10}{"Events/rd",12}{"Events/visit",14}");

        var totalMinutes = TotalTime.TotalMinutes;
        foreach (var stateId in stateIds)
        {
            var state = States[stateId];
            var percent = totalMinutes <= 0d ? 0d : state.Time.TotalMinutes / totalMinutes * 100d;
            var perVisit = state.Visits == 0 ? 0f : state.Events / (float)state.Visits;

            builder.AppendLine(
                $"{stateId,-16}{state.Visits / (float)rounds,12:F2}{state.Time.TotalMinutes / rounds,9:F1} min" +
                $"{percent,9:F1}%{state.Events / (float)rounds,12:F2}{perVisit,14:F2}");
        }

        builder.AppendLine(
            $"{"TOTAL",-16}{"",12}{totalMinutes / rounds,9:F1} min{"",10}{EventsPerRound,12:F2}");

        builder.AppendLine();
        builder.Append($"{"Event",-40}{"Total/rd",10}{"Rounds 1+",12}");
        foreach (var stateId in stateIds)
        {
            builder.Append($"{stateId,12}");
        }

        builder.AppendLine();

        var ranEvents = RoundsWithEvent.Keys
            .OrderByDescending(id => States.Values.Sum(state => state.EventCounts.GetValueOrDefault(id)))
            .ThenBy(id => id);

        foreach (var eventId in ranEvents)
        {
            var total = States.Values.Sum(state => state.EventCounts.GetValueOrDefault(eventId));

            builder.Append($"{eventId,-40}{total / (float)rounds,10:F2}" +
                           $"{RoundsWithEvent[eventId] / (float)rounds * 100f,11:F1}%");

            foreach (var stateId in stateIds)
            {
                builder.Append($"{States[stateId].EventCounts.GetValueOrDefault(eventId) / (float)rounds,12:F2}");
            }

            builder.AppendLine();
        }

        var neverFired = NeverFired.ToList();
        if (neverFired.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine($"Never fired ({neverFired.Count}): {string.Join(", ", neverFired)}");
        }

        return builder.ToString();
    }
}
