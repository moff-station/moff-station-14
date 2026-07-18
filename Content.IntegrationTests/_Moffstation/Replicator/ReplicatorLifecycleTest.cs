#nullable enable

using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server._Moffstation.GameTicking.Rules;
using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Shared._Moffstation.Replicator.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests._Moffstation.Replicator;

/// <summary>
/// Basic integration smoke test for replicators from rule addition, spawning, nest creating, to point accumulation.
/// </summary>
[TestFixture, TestOf(typeof(ReplicatorRuleSystem))]
public sealed class ReplicatorLifecycleTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        DummyTicker = false,
        Connected = true,
        Map = PoolManager.TestStation,
    };

    private const string RuleId = "TestReplicatorRule";

    /// Mirrors the real ReplicatorSpawn event (minus StationEvent), so that starting the rule
    /// automatically spawns MobReplicatorT0Queen and places the SpawnPointGhostReplicatorQueenAntag marker.
    [TestPrototypes]
    private const string TestProtos = $@"
- type: entity
  id: {RuleId}
  parent: BaseGameRule
  suffix: TEST
  components:
  - type: ReplicatorRule
  - type: GameRule
  - type: AntagRandomSpawn
  - type: AntagSpawner
    prototype: MobReplicatorT0Queen
  - type: AntagSelection
    agentName: ghost-role-information-replicator-name
    antags:
    - !type:FixedAntagCount
      proto: ReplicatorQueen
";

    [SidedDependency(Side.Server)] private readonly GameTicker _ticker = default!;
    [SidedDependency(Side.Server)] private readonly GhostRoleSystem _ghostRole = default!;
    [SidedDependency(Side.Server)] private readonly SharedActionsSystem _action = default!;

    [Test]
    public async Task TestReplicatorLifecycle()
    {
        // Add and start rule.
        var ruleEntity = EntityUid.Invalid;
        await Server.WaitPost(() =>
        {
            Assert.That(
                _ticker.StartGameRule(RuleId, out ruleEntity),
                "TestReplicatorRule should start successfully"
            );
        });

        // Find rule-created spawner.
        var queenSpawner = EntityUid.Invalid;
        await Server.WaitAssertion(() =>
        {
            var enumerator = SEntMan.EntityQueryEnumerator<GhostRoleAntagSpawnerComponent, GhostRoleComponent>();
            Assert.That(
                enumerator.MoveNext(out queenSpawner, out var grasComp, out _),
                "AntagSelection should have created a GhostRoleAntagSpawner for the queen"
            );
            Assert.That(
                grasComp!.Definition!.Value.Id,
                Is.EqualTo("ReplicatorQueen"),
                "Spawner should be for the ReplicatorQueen antagSpecifier"
            );
        });

        // Take the spawner.
        var queen = EntityUid.Invalid;
        await Server.WaitPost(() =>
        {
            var identifier = SComp<GhostRoleComponent>(queenSpawner).Identifier;
            Assert.That(
                _ghostRole.Takeover(ServerSession!, identifier),
                "Player should successfully take the ghost role"
            );
            Assert.That(
                ServerSession!.AttachedEntity,
                Is.Not.Null,
                "Player session should be attached to the queen after taking the ghost role"
            );
            queen = ServerSession.AttachedEntity!.Value;

            Assert.That(
                SEntMan.HasComponent<ReplicatorQueenComponent>(queen),
                "Session-attached entity should have ReplicatorQueenComponent"
            );
        });

        // Spawn the nest.
        await Server.WaitPost(() =>
        {
            var nestActionEnt = SComp<ReplicatorQueenComponent>(queen).SpawnNestActionEnt;
            var nestAction = _action.GetAction(nestActionEnt)!.Value;
            _action.PerformAction(queen, nestAction);
        });

        // Wait for deferred deletions
        await RunTicksSync(2);

        var t1Replicator = EntityUid.Invalid;
        var nest = EntityUid.Invalid;
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(queen), "T0 queen entity should be deleted after nest creation");

            Assert.That(
                ServerSession!.AttachedEntity,
                Is.Not.Null,
                "Player session should be attached to the T1 replicator after nest creation"
            );
            t1Replicator = ServerSession.AttachedEntity!.Value;

            Assert.That(
                SEntMan.GetComponent<MetaDataComponent>(t1Replicator).EntityPrototype?.ID,
                Is.EqualTo("MobReplicatorTier1"),
                "Session-attached entity should be the T1 replicator"
            );

            var nestQuery = SEntMan.EntityQueryEnumerator<ReplicatorNestComponent>();
            Assert.That(nestQuery.MoveNext(out nest, out _), "Nest should exist after queen action");

            var rule = SComp<ReplicatorRuleComponent>(ruleEntity);
            Assert.That(rule.NestCount, Is.EqualTo(1), "Rule should track the nest creation (NestCount = 1)");
        });

        // Consume a steel stack to generate points, level up the nest, and trigger spawner creation.
        await Server.WaitPost(() =>
        {
            var nestCoords = SEntMan.GetComponent<TransformComponent>(nest).Coordinates;
            SSpawnAtPosition("SheetSteel", nestCoords);
        });

        // Wait for the steel to fall into the hole.
        await RunTicksSync(60);

        await Server.WaitAssertion(() =>
        {
            var nestComp = SComp<ReplicatorNestComponent>(nest);
            Assert.That(nestComp.CurrentLevel, Is.EqualTo(2), "Nest should have leveled up to level 2");
            Assert.That(
                nestComp.UnclaimedSpawners.Count,
                Is.GreaterThanOrEqualTo(1),
                "At least one replicator spawner should have been created"
            );

            var rule = SComp<ReplicatorRuleComponent>(ruleEntity);
            Assert.That(rule.TotalSizePoints, Is.EqualTo(30), "Rule should track all gained size points");
            Assert.That(rule.TotalSpawnersCreated, Is.EqualTo(1), "Rule should track one spawner creation");
        });

        // Upgrade T1 → T2 Combat.

        await Server.WaitPost(() =>
        {
            Entity<ActionComponent>? upgradeAction = null;
            foreach (var action in _action.GetActions(t1Replicator))
            {
                if (SEntMan.GetComponent<MetaDataComponent>(action).EntityPrototype?.ID ==
                    "ActionReplicatorUpgrade2Alt")
                {
                    upgradeAction = action;
                    break;
                }
            }

            Assert.That(upgradeAction, Is.Not.Null, "T1 should have ActionReplicatorUpgrade2Alt");
            _action.PerformAction(t1Replicator, upgradeAction!.Value);
        });

        await RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(t1Replicator), "T1 replicator should be deleted after upgrade");

            // UpgradeReplicator transfers the mind synchronously, so the session is now attached to the T2.
            Assert.That(
                ServerSession!.AttachedEntity,
                Is.Not.Null,
                "Player session should be attached to the T2 replicator after upgrade"
            );
            var t2Uid = ServerSession.AttachedEntity!.Value;
            Assert.That(
                SEntMan.GetComponent<MetaDataComponent>(t2Uid).EntityPrototype?.ID,
                Is.EqualTo("MobReplicatorTier2Combat"),
                "Session-attached entity should be the T2 replicator"
            );
        });

        // Verify final rule state.

        await Server.WaitAssertion(() =>
        {
            var rule = SComp<ReplicatorRuleComponent>(ruleEntity);
            Assert.Multiple(() =>
            {
                Assert.That(rule.NestCount, Is.EqualTo(1), "Final rule NestCount should be 1");
                Assert.That(rule.TotalSizePoints, Is.EqualTo(30), "Final rule TotalSizePoints should be 30");
                Assert.That(rule.TotalSpawnersCreated, Is.EqualTo(1), "Final rule TotalSpawnersCreated should be 1");
            });
        });

        // Verify round-end text is generated.

        await Server.WaitAssertion(() =>
        {
            var textEv = new RoundEndTextAppendEvent();
            SEntMan.EventBus.RaiseEvent(EventSource.Local, textEv);

            Assert.That(textEv.Text, Does.Contain("Nest(s)"), "Round-end text should mention nests");
            Assert.That(textEv.Text, Does.Contain("Replicators"), "Round-end text should mention replicators");
            Assert.That(textEv.Text, Does.Contain("size points"), "Round-end text should mention size points");
        });
    }
}
