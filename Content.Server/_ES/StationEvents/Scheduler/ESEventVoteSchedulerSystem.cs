using System.Linq;
using Content.Server._ES.StationEvents.Scheduler.Components;
using Content.Server._ES.Voting;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents;
using Content.Shared._ES.Voting.Components;
using Content.Shared._ES.Voting.Results;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.StationEvents.Scheduler;

public sealed class ESEventVoteSchedulerSystem : GameRuleSystem<ESEventVoteSchedulerComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly EventManagerSystem _eventManager = default!;
    [Dependency] private readonly ESVoteSystem _esVote = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESEventVoteComponent, ESGetVoteOptionsEvent>(OnGetVoteOptions);
        SubscribeLocalEvent<ESEventVoteComponent, ESVoteCompletedEvent>(OnVoteCompleted);
    }

    protected override void Started(EntityUid uid,
        ESEventVoteSchedulerComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.NextEventTime = Timing.CurTime
                                  + component.BaseFirstEventDelay
                                  + RobustRandom.Next(component.MinEventDelay, component.MaxEventDelay);
    }

    protected override void ActiveTick(EntityUid uid, ESEventVoteSchedulerComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (Timing.CurTime < component.NextEventTime)
            return;
        component.NextEventTime += RobustRandom.Next(component.MinEventDelay, component.MaxEventDelay);

        // This is genuinely a terrible API
        var availableEvents = _eventManager.AvailableEvents();
        if (!_eventManager.TryBuildLimitedEvents(component.EventTable, availableEvents, out var limitedEvents))
            return; // DIE ! ! !

        var vote = Spawn(component.VotePrototype);
        var comp = EnsureComp<ESEventVoteComponent>(vote);

        var optionCount = Math.Min(component.VoteOptions, limitedEvents.Count); // Drop the amount of options if we don't have enough
        var weightedEvents = limitedEvents.Select(pair => (pair.Key, pair.Value.Weight)).ToDictionary();
        for (var i = 0; i < optionCount; ++i)
        {
            // No duplicates!
            comp.EventIds.Add(RobustRandom.PickAndTake(weightedEvents));
        }

        // Refresh options so we get stuff from ESEventVoteComponent
        _esVote.RefreshVoteOptions(vote);
    }

    private void OnGetVoteOptions(Entity<ESEventVoteComponent> ent, ref ESGetVoteOptionsEvent args)
    {
        foreach (var protoId in ent.Comp.EventIds)
        {
            args.Options.Add(new ESEntityPrototypeVoteOption(_prototypeManager.Index(protoId)));
        }
    }

    private void OnVoteCompleted(Entity<ESEventVoteComponent> ent, ref ESVoteCompletedEvent args)
    {
        if (args.Result is not ESEntityPrototypeVoteOption proto)
            return;
        GameTicker.AddGameRule(proto.Entity);
    }
}
