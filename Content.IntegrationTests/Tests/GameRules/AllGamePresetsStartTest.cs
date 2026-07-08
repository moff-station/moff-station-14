using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.Shuttles.Components;
using Content.Shared.Antag;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.GameRules;

[TestFixture]
public sealed class AllGamePresetsStartTest : AntagTest
{
    /// <summary>
    /// A list of blacklisted <see cref="GamePresetPrototype"/> for this test. Some down streams might make changes which nuke upstream game modes they don't use.
    /// This prevents them from being tested. If you use this to silence valid test fails and your game fails to start. Skill issue. Do 100 push-ups.
    /// </summary>
    private static readonly HashSet<string> IgnoredPresets = []; // Is a string to prevent YAML Linter from freaking if this is empty.

    private static string[] _gamePresets = GameDataScrounger.PrototypesOfKind<GamePresetPrototype>().Where(p => !IgnoredPresets.Contains(p)).ToArray();

    // Tests that all game modes can start given ideal circumstances.
    [Test]
    [TestOf(typeof(GameTicker)), TestOf(typeof(AntagSelectionSystem)), TestOf(typeof(AntagSelectionComponent))]
    [TestCaseSource(nameof(_gamePresets))]
    [Description("Ensures all Game Presets are able to start and assign all antags correctly without spawning anyone in nullspace.")]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GameTickerIgnoredPresets), GameTicker.DummyGameRule)]
    public async Task TestAllGamemodesCanStart(string presetId)
    {
        // Initially in the lobby
        await Server.WaitPost(() =>
        {
            Assert.That(STicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
            Assert.That(Client.AttachedEntity, Is.Null);
            Assert.That(STicker.PlayerGameStatuses[Client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));
        });

        var preset = SProtoMan.Index<GamePresetPrototype>(presetId);

        // Spawn the minimum number of players.
        var players = new List<ICommonSession>();
        players.Add(Client.Session);
        var min = 0;
        await Server.WaitPost(() =>
        {
            min = STicker.GetMinimumPlayerCount(preset);
        });

        // We should already have one client connected, and we need to check the min

        // If we have antags, make sure that those with the correct preferences can spawn with them!
        List<(AntagSpecifierPrototype, int)> rules = [];

        var antags = 0;
        await Server.WaitPost(() =>
        {
            foreach (var ruleId in preset.Rules)
            {
                if (STicker.IsIgnored(ruleId))
                    continue;

                if (!SProtoMan.Resolve(ruleId, out var rule ))
                    continue; // Bruh moment

                // Ignore non-antag game-rules.
                if (!rule.TryGetComponent<AntagSelectionComponent>(out var antag, SEntMan.ComponentFactory))
                    continue;

                var runningCount = 0;

                foreach (var selector in antag.Antags)
                {
                    // Throw on invalid prototypes, skip roundstart ghost roles.
                    if (!SProtoMan.Resolve(selector.Proto, out var definition) || definition.PrefRoles.Count == 0)
                        continue;

                    var count = AntagSys.GetTargetAntagCount(selector, min, ref runningCount);
                    antags += count;
                    rules.Add((definition, count));
                }
            }
        });

        // No preset should ever try to spawn more antags roundstart than it can spawn players.
        Assert.That(antags <= min, Is.True);
        if (min > 1)
        {
            var dummies = await Server.AddDummySessions(min - 1);
            // Put our client at the front of the list.
            players = players.Union(dummies).ToList();
        }

        await Pair.RunUntilSynced();

        // This also ensures that admin commands work properly :P
        await Server.WaitPost(() =>
        {
            STicker.ToggleReadyAll(true);
        });

        var i = 0;
        foreach (var (antag, amount) in rules)
        {
            for (var count = 0; count < amount; count++)
            {
                await Pair.SetAntagPreference(antag.PrefRoles.FirstOrDefault(), true, players[i++].UserId);
                Assert.That(i < min, $"Tried to assign more antags than there were players");
            }
        }

        await Pair.RunUntilSynced();
        await Pair.WaitCommand($"setgamepreset {presetId}");
        await Pair.WaitCommand("startround");
        await Pair.RunUntilSynced();

        // Game should have started
        await Server.WaitPost(() =>
        {
            Assert.That(STicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
            Assert.That(STicker.PlayerGameStatuses.Values.All(x => x == PlayerGameStatus.JoinedGame));
            Assert.That(STicker.PlayerGameStatuses, Has.Count.EqualTo(players.Count));
        });
        Assert.That(CEntMan.EntityExists(Client.AttachedEntity));

        var player = Pair.Player!.AttachedEntity!.Value;
        Assert.That(SEntMan.EntityExists(player));

        // Start all game presets so antags spawn!
        await Server.WaitPost(() =>
        {
            STicker.StartGamePresetRules();
        });
        await Pair.RunUntilSynced();

        await Server.WaitPost(() =>
        {
            // var j = 0; // Moffstation - Fuck this J in particular (for not being used)
            foreach (var (antag, amount) in rules)
            {
                for (var count = 0; count < amount; count++)
                {
                    // Moffstation - Begin - Swap to verification which is tolerate of out-of-order antags.
                    // AssertAntagInitialized(antag, players[j++]);
                    AssertAntagInitializedFlexible(antag, ref players);
                    // Moffstation - End
                }
            }
        });

        // Maps now exist
        Assert.That(SEntMan.Count<MapComponent>(), Is.GreaterThan(0));
        Assert.That(SEntMan.Count<MapGridComponent>(), Is.GreaterThan(0));
        Assert.That(SEntMan.Count<StationCentcommComponent>(), Is.EqualTo(1));

        // Clear game preset and return to lobby
        await Pair.WaitCommand("golobby");
        STicker.SetGamePreset((GamePresetPrototype) null);
        await Pair.RunUntilSynced();

        // Moffstation - Begin - Modifies the test to be able to handle out-of-order antags. All of upstream seems to depend on a specific order or specific set of distinct antags rolling. Moffstation has different antags, so we need the test to be a little more flexible. Woe upon ye for needing such intense shitcode to be a LITTLE more flexible.
        #nullable enable
        void AssertAntagInitializedFlexible(AntagSpecifierPrototype antag, ref List<ICommonSession> players)
        {
            var playersAndAssertionFailures = players.Select(player => (player, AssertAntagInitialized(antag, player))).ToList();
            if (playersAndAssertionFailures.FirstOrNull(x => x.Item2 != null)?.Item1 is { } passingPlayer)
            {
                players.Remove(passingPlayer);
                return;
            }

            using (Assert.EnterMultipleScope())
            {
                playersAndAssertionFailures.ForEach(it => it.Item2?.Invoke());
            }
        }
        Action? AssertAntagInitialized(AntagSpecifierPrototype antag, ICommonSession session)
        {
            if (!SMind.TryGetMind(session, out var mindEnt, out var mindComp))
                return () => Assert.Fail($"Session {session} spawned into the game as an antag but had no mind!");
            if (!SEntMan.EntityExists(mindComp!.CurrentEntity))
                return () => Assert.Fail($"Session {session} spawned into the game as an antag, but had no entity!");
            var ent = mindComp.CurrentEntity!.Value;

            // We don't necessarily know if an antag should spawn on the station, but we know they shouldn't spawn in nullspace.
            var xform = SEntMan.GetComponent<TransformComponent>(ent);
            Assert.That(xform.MapUid, Is.Not.Null);
            Assert.That(xform.MapID, Is.Not.EqualTo(MapId.Nullspace));

            // Make sure all components were added
            foreach (var comp in antag.Components)
            {
                // Moffstation - Some components remove themselves on initialization and thus cannot be checked for here, so we add a list of components to not check for.
                if (antag.DoNotCheckComponents.Contains(comp.Key))
                    continue;

                if (!SEntMan.HasComponent(ent, comp.Value.Component.GetType()))
                    return () => Assert.Fail($"Entity {SEntMan.ToPrettyString(ent)} owned by {session} failed to acquire {comp.Key} component, while becoming {antag.ID}");
            }

            // Make sure all mind components were added
            foreach (var comp in antag.MindComponents)
            {
                if (!SEntMan.HasComponent(mindEnt, comp.Value.Component.GetType()))
                    return () => Assert.Fail($"Mind {SEntMan.ToPrettyString(mindEnt)} owned by {session} failed to acquire {comp.Key} component, while becoming {antag.ID}");
            }

            if (antag.MindRoles != null)
            {
                foreach (var role in antag.MindRoles)
                {
                    var condition = mindComp.MindRoleContainer.ContainedEntities.Any(x =>
                        SEntMan.GetComponent<MetaDataComponent>(x).EntityPrototype?.ID! == role);
                    if (!condition)
                        return () => Assert.Fail($"{SToPrettyString(mindEnt)} owned by {session}, failed to acquire role {role} for antagonist {antag}");
                }
            }

            return null;
        }
        #nullable restore
        // Moffstation - End
    }
}
