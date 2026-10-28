using Content.Server._Moffstation.StationEvents.Components;
using Content.Server.StationEvents;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Random.Helpers;

namespace Content.Server._Moffstation.StationEvents;

public sealed partial class MoffStationEventSchedulerSystem : GameRuleSystem<MoffStationEventSchedulerComponent>
{
    [Dependency] private EventManagerSystem _event = default!;

    protected override void Started(Entity<MoffStationEventSchedulerComponent, GameRuleComponent> rule, ref GameRuleStartedEvent args)
    {
        base.Started(rule, ref args);
        var (uid, component, _) = rule;

        if (!component.States.TryGetValue(component.InitialState, out var state))
        {
            Log.Error($"{ToPrettyString(uid)} has no state named \"{component.InitialState}\" to start in!");
            return;
        }

        EnterState((uid, component), component.InitialState);
        // Overrides the first event's time
        component.NextEventTime = Timing.CurTime + TimeSpan.FromSeconds(component.InitialDelaySeconds.Next(RobustRandom));
    }

    protected override void Ended(Entity<MoffStationEventSchedulerComponent> rule, ref GameRuleEndedEvent args)
    {
        base.Ended(rule, ref args);

        rule.Comp.CurrentStateId = null;
        rule.Comp.NextEventTime = TimeSpan.Zero;
        rule.Comp.NextStateTime = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_event.EventsEnabled)
            return;

        foreach (var ent in EntityQueryEnumerator<MoffStationEventSchedulerComponent, GameRuleComponent>())
        {
            if (!GameTicker.IsGameRuleActive((ent.Owner, ent.Comp2)))
                continue;

            if (ent.Comp1.CurrentState is not { } state)
                continue;

            if (ent.Comp1.NextStateTime <= Timing.CurTime)
            {
                TransitionToNextState(ent, state);
                continue;
            }

            if (state.MinMaxEventTiming is not { } eventTiming)
                continue;

            if (ent.Comp1.NextEventTime > Timing.CurTime)
                continue;

            ent.Comp1.NextEventTime = Timing.CurTime + TimeSpan.FromSeconds(eventTiming.Next(RobustRandom));
            _event.RunRandomEvent(ent.Comp1.ScheduledGameRules);
        }
    }

    private string? PickNextState(MoffEventSchedulerState state)
    {
        return state.NextStates.Count == 0 ? null : RobustRandom.Pick(state.NextStates);
    }

    private void TransitionToNextState(Entity<MoffStationEventSchedulerComponent> ent, MoffEventSchedulerState currentState)
    {
        if (currentState.EventOnEnd)
        {
            _event.RunRandomEvent(ent.Comp.ScheduledGameRules);
        }

        if (PickNextState(currentState) is not { } nextId)
        {
            // Nowhere to go, so stay put for the rest of the round.
            ent.Comp.NextStateTime = null;
            return;
        }

        if (!ent.Comp.States.TryGetValue(nextId, out var next))
        {
            Log.Error($"{ToPrettyString(ent.Owner)} tried to enter unknown state \"{nextId}\"!");
            ent.Comp.NextStateTime = null;
            return;
        }

        EnterState(ent, nextId);
    }

    private void EnterState(Entity<MoffStationEventSchedulerComponent> entity, string id)
    {
        if (!entity.Comp.States.TryGetValue(id, out var next))
        {
            this.AssertOrLogError($"{ToPrettyString(entity)} tried to enter unknown state \"{id}\"!");
            entity.Comp.NextStateTime = null;
            return;
        }

        entity.Comp.CurrentStateId = id;

        // These are absolute times, not durations, since Update compares them against CurTime.
        var duration = next.Duration is { } stateDuration
            ? TimeSpan.FromSeconds(stateDuration.Next(RobustRandom))
            : (TimeSpan?)null;

        var eventDelay = next.MinMaxEventTiming is { } eventTiming
            ? TimeSpan.FromSeconds(eventTiming.Next(RobustRandom))
            : TimeSpan.Zero;

        entity.Comp.NextStateTime = duration is { } d ? Timing.CurTime + d : null;
        entity.Comp.NextEventTime = Timing.CurTime + eventDelay;
    }
}
