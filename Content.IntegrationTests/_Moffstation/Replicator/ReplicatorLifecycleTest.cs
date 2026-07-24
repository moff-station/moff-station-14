#nullable enable

using System.Linq;
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
using Content.Shared.Maps;
using Content.Shared.Stacks;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

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
    [SidedDependency(Side.Server)] private readonly TurfSystem _turf = default!;
    [SidedDependency(Side.Server)] private readonly TileSystem _tile = default!;
    [SidedDependency(Side.Server)] private readonly SharedToolSystem _tool = default!;
    [SidedDependency(Side.Server)] private readonly EntityLookupSystem _lookup = default!;
    [SidedDependency(Side.Server)] private readonly ITileDefinitionManager _tileDefMan = default!;

    private EntityUid _ruleEntity;
    private EntityUid _nest;
    private EntityUid _t1Replicator;
    private EntityCoordinates _nestCoords;

    /// <summary>
    /// Starts the rule, takes the queen ghost role, and spawns the nest, leaving a T1 replicator and its
    /// nest ready for the individual lifecycle tests below.
    /// </summary>
    [SetUp]
    public async Task SetUpNest()
    {
        var ruleEntity = EntityUid.Invalid;
        await Server.WaitPost(() =>
        {
            Assume.That(
                _ticker.StartGameRule(RuleId, out ruleEntity),
                "TestReplicatorRule should start successfully"
            );
        });

        var queenSpawner = EntityUid.Invalid;
        await Server.WaitAssertion(() =>
        {
            var enumerator = SEntMan.EntityQueryEnumerator<GhostRoleAntagSpawnerComponent, GhostRoleComponent>();
            Assume.That(
                enumerator.MoveNext(out queenSpawner, out var grasComp, out _),
                "AntagSelection should have created a GhostRoleAntagSpawner for the queen"
            );
            Assume.That(
                grasComp!.Definition!.Value.Id,
                Is.EqualTo("ReplicatorQueen"),
                "Spawner should be for the ReplicatorQueen antagSpecifier"
            );
        });

        var queen = EntityUid.Invalid;
        await Server.WaitPost(() =>
        {
            var identifier = SComp<GhostRoleComponent>(queenSpawner).Identifier;
            Assume.That(
                _ghostRole.Takeover(ServerSession!, identifier),
                "Player should successfully take the ghost role"
            );
            Assume.That(
                ServerSession!.AttachedEntity,
                Is.Not.Null,
                "Player session should be attached to the queen after taking the ghost role"
            );
            queen = ServerSession.AttachedEntity!.Value;

            Assume.That(
                SEntMan.HasComponent<ReplicatorQueenComponent>(queen),
                "Session-attached entity should have ReplicatorQueenComponent"
            );
        });

        await Server.WaitPost(() =>
        {
            var nestActionEnt = SComp<ReplicatorQueenComponent>(queen).SpawnNestActionEnt;
            var nestAction = _action.GetAction(nestActionEnt)!.Value;
            _action.PerformAction(queen, nestAction);
        });

        // Wait for deferred deletions.
        await RunTicksSync(2);

        var t1Replicator = EntityUid.Invalid;
        var nest = EntityUid.Invalid;
        await Server.WaitAssertion(() =>
        {
            Assume.That(SEntMan.Deleted(queen), "T0 queen entity should be deleted after nest creation");

            Assume.That(
                ServerSession!.AttachedEntity,
                Is.Not.Null,
                "Player session should be attached to the T1 replicator after nest creation"
            );
            t1Replicator = ServerSession.AttachedEntity!.Value;

            Assume.That(
                SEntMan.GetComponent<MetaDataComponent>(t1Replicator).EntityPrototype?.ID,
                Is.EqualTo("MobReplicatorTier1"),
                "Session-attached entity should be the T1 replicator"
            );

            var nestQuery = SEntMan.EntityQueryEnumerator<ReplicatorNestComponent>();
            Assume.That(nestQuery.MoveNext(out nest, out _), "Nest should exist after queen action");

            var rule = SComp<ReplicatorRuleComponent>(ruleEntity);
            Assume.That(rule.NestCount, Is.EqualTo(1), "Rule should track the nest creation (NestCount = 1)");
        });

        _ruleEntity = ruleEntity;
        _nest = nest;
        _t1Replicator = t1Replicator;

        await Server.WaitPost(() =>
        {
            _nestCoords = SEntMan.GetComponent<TransformComponent>(_nest).Coordinates;
        });
    }

    /// <summary>
    /// Clears the game rule at the end of each test, mirroring how a round would end.
    /// </summary>
    [TearDown]
    public async Task TearDownRule()
    {
        await Server.WaitPost(() => _ticker.ClearGameRules());
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void TestWeldReplicatorTileYieldsMaterials()
    {
        Assume.That(
            _tileDefMan.TryGetDefinition("FloorReplicator", out var replicatorTileDef),
            "FloorReplicator tile definition should exist"
        );

        var tileRef = _turf.GetTileRef(_nestCoords)!.Value;
        var originalTileTypeId = tileRef.Tile.TypeId;
        _tile.ReplaceTile(tileRef, (ContentTileDefinition) replicatorTileDef!);

        tileRef = _turf.GetTileRef(_nestCoords)!.Value;
        Assume.That(
            tileRef.Tile.TypeId,
            Is.EqualTo(((ContentTileDefinition) replicatorTileDef!).TileId),
            "Tile should be FloorReplicator after replacement"
        );

        var welder = SSpawnAtPosition("Welder", _nestCoords);
        var qualities = SComp<ToolComponent>(welder).Qualities;
        Assert.That(
            _tool.TryDeconstructWithToolQualities(tileRef, qualities),
            "Welding a FloorReplicator tile should succeed"
        );

        var newTileRef = _turf.GetTileRef(_nestCoords)!.Value;
        Assert.That(
            newTileRef.Tile.TypeId,
            Is.EqualTo(originalTileTypeId),
            "Tile should revert to its pre-conversion tile after welding"
        );

        var materials = _lookup.GetEntitiesInRange<StackComponent>(_nestCoords, 1f)
            .Where(e => e.Comp.StackTypeId == "Steel" || e.Comp.StackTypeId == "Plasteel")
            .ToList();
        Assert.That(materials, Is.Not.Empty, "Welding a FloorReplicator tile should drop steel or plasteel");

        var totalCount = materials.Sum(e => e.Comp.Count);
        Assert.That(
            totalCount,
            Is.InRange(1, 5),
            "Welding a FloorReplicator tile should yield between 1 and 5 material sheets"
        );
    }

    [Test]
    public async Task TestNestLevelsUpAndUpgradesToTier2Combat()
    {
        // Consume a steel stack to generate points, level up the nest, and trigger spawner creation.
        await Server.WaitPost(() =>
        {
            SSpawnAtPosition("SheetSteel", _nestCoords);
        });

        // Wait for the steel to fall into the hole.
        await RunTicksSync(60);

        await Server.WaitAssertion(() =>
        {
            var nestComp = SComp<ReplicatorNestComponent>(_nest);
            Assert.That(nestComp.CurrentLevel, Is.EqualTo(2), "Nest should have leveled up to level 2");
            Assert.That(
                nestComp.UnclaimedSpawners.Count,
                Is.GreaterThanOrEqualTo(1),
                "At least one replicator spawner should have been created"
            );

            var rule = SComp<ReplicatorRuleComponent>(_ruleEntity);
            Assert.That(rule.TotalSizePoints, Is.EqualTo(30), "Rule should track all gained size points");
            Assert.That(rule.TotalSpawnersCreated, Is.EqualTo(1), "Rule should track one spawner creation");
        });

        // Upgrading to T2 Combat claims one of the spawners the leveling above just created, so it
        // must happen afterward.
        await Server.WaitPost(() =>
        {
            Entity<ActionComponent>? upgradeAction = null;
            foreach (var action in _action.GetActions(_t1Replicator))
            {
                if (SEntMan.GetComponent<MetaDataComponent>(action).EntityPrototype?.ID ==
                    "ActionReplicatorUpgrade2Alt")
                {
                    upgradeAction = action;
                    break;
                }
            }

            Assume.That(upgradeAction, Is.Not.Null, "T1 should have ActionReplicatorUpgrade2Alt");
            _action.PerformAction(_t1Replicator, upgradeAction!.Value);
        });

        // Wait for deferred deletions.
        await RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(_t1Replicator), "T1 replicator should be deleted after upgrade");

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
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void TestRoundEndTextMentionsReplicators()
    {
        var textEv = new RoundEndTextAppendEvent();
        SEntMan.EventBus.RaiseEvent(EventSource.Local, textEv);

        Assert.That(textEv.Text, Does.Contain("Nest(s)"), "Round-end text should mention nests");
        Assert.That(textEv.Text, Does.Contain("Replicators"), "Round-end text should mention replicators");
        Assert.That(textEv.Text, Does.Contain("size points"), "Round-end text should mention size points");
    }
}
