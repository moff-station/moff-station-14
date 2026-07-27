using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared._Moffstation.Replicator;
using Content.Shared._Moffstation.Replicator.Components;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Moffstation.GameTicking.Rules;

/// Maintains the stats in <see cref="ReplicatorRuleComponent"/> and handles displaying the round-end text.
public sealed partial class ReplicatorRuleSystem : GameRuleSystem<ReplicatorRuleComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplicatorNestComponent, ReplicatorNestCreatedEvent>(OnNestCreated);
        SubscribeLocalEvent<ReplicatorNestComponent, ReplicatorNestSizePointsGainedEvent>(OnSizePointsGained);
        SubscribeLocalEvent<ReplicatorNestComponent, ReplicatorSpawnerCreatedEvent>(OnSpawnerCreated);
    }

    protected override void AppendRoundEndText(
        EntityUid uid,
        ReplicatorRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        args.AddLine(Loc.GetString("replicator-nest-end-of-round",
            ("nests", component.NestCount),
            ("replicators", component.TotalSpawnersCreated),
            ("points", component.TotalSizePoints)));
    }

    private void OnNestCreated(Entity<ReplicatorNestComponent> ent, ref ReplicatorNestCreatedEvent args)
    {
        UpdateActiveRule(rule => rule.NestCount++);
    }

    private void OnSizePointsGained(Entity<ReplicatorNestComponent> ent, ref ReplicatorNestSizePointsGainedEvent args)
    {
        var points = args.Points;
        UpdateActiveRule(rule => rule.TotalSizePoints += points);
    }

    private void OnSpawnerCreated(Entity<ReplicatorNestComponent> ent, ref ReplicatorSpawnerCreatedEvent args)
    {
        UpdateActiveRule(rule => rule.TotalSpawnersCreated++);
    }

    private void UpdateActiveRule(Action<ReplicatorRuleComponent> update)
    {
        var query = EntityQueryEnumerator<ReplicatorRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var rule, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            update(rule);
            break;
        }
    }
}
