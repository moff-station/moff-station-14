using Content.Server._Moffstation.StationEvents.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.StationEvents;

public sealed partial class MoffStationEventSchedulerSystem : GameRuleSystem<MoffStationEventSchedulerComponent>
{
    [Dependency] private EventManagerSystem _event = default!;
    [Dependency] private IGameTiming _timing = default!;

    protected override void Started(
        EntityUid uid,
        MoffStationEventSchedulerComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!component.States.TryGetValue(component.InitialState, out var state))
        {
            Log.Error($"{ToPrettyString(uid)} has no state named \"{component.InitialState}\" to start in!");
            return;
        }

        EnterState(component, component.InitialState, state);
        component.NextEventTime = _timing.CurTime + TimeSpan.FromSeconds(component.InitialDelay.Next(RobustRandom));
    }

    protected override void Ended(
        EntityUid uid,
        MoffStationEventSchedulerComponent component,
        GameRuleComponent gameRule,
        GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        component.CurrentState = null;
        component.NextEventTime = TimeSpan.Zero;
        component.NextStateTime = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_event.EventsEnabled)
            return;

        foreach (var ent in EntityQueryEnumerator<MoffStationEventSchedulerComponent, GameRuleComponent>())
        {
            if (ent.Comp1.CurrentState is not { } stateId || !ent.Comp1.States.TryGetValue(stateId, out var state))
                continue;

            if (ent.Comp1.NextStateTime <= _timing.CurTime)
            {
                if (state.EventOnEnd)
                    _event.RunRandomEvent(ent.Comp1.ScheduledGameRules);

                ChangeState(ent.Owner, ent.Comp1, state);
                continue;
            }

            if (state.MinMaxEventTiming is not { } eventTiming)
                continue;

            if (ent.Comp1.NextEventTime > _timing.CurTime)
                continue;

            ent.Comp1.NextEventTime = _timing.CurTime + TimeSpan.FromSeconds(eventTiming.Next(RobustRandom));
            _event.RunRandomEvent(ent.Comp1.ScheduledGameRules);
        }
    }

    private string? PickNextState(MoffEventSchedulerState state)
    {
        return state.NextStates.Count == 0 ? null : RobustRandom.Pick(state.NextStates);
    }

    private (TimeSpan? Duration, TimeSpan EventDelay) RollState(MoffEventSchedulerState state)
    {
        // A state can have a duration without event timing, or the other way around, so these roll separately.
        var duration = state.Duration is { } stateDuration
            ? TimeSpan.FromSeconds(stateDuration.Next(RobustRandom))
            : (TimeSpan?)null;

        var eventDelay = state.MinMaxEventTiming is { } eventTiming
            ? TimeSpan.FromSeconds(eventTiming.Next(RobustRandom))
            : TimeSpan.Zero;

        return (duration, eventDelay);
    }

    private void ChangeState(EntityUid uid, MoffStationEventSchedulerComponent component, MoffEventSchedulerState state)
    {
        if (PickNextState(state) is not { } nextId)
        {
            // Nowhere to go, so stay put for the rest of the round.
            component.NextStateTime = null;
            return;
        }

        if (!component.States.TryGetValue(nextId, out var next))
        {
            Log.Error($"{ToPrettyString(uid)} tried to enter unknown state \"{nextId}\"!");
            component.NextStateTime = null;
            return;
        }

        EnterState(component, nextId, next);
    }

    private void EnterState(MoffStationEventSchedulerComponent component, string id, MoffEventSchedulerState state)
    {
        component.CurrentState = id;

        // These are absolute times, not durations, since Update compares them against CurTime.
        var (duration, eventDelay) = RollState(state);
        component.NextStateTime = duration is { } stateDuration ? _timing.CurTime + stateDuration : null;
        component.NextEventTime = _timing.CurTime + eventDelay;
    }
}
