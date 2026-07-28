#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Pair;
using Content.Server._Moffstation.Preferences;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Preferences.Managers;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Moffstation.Round;

/// <summary>
/// Job selection is per character, job priority is per player, and only active characters spawn.
/// </summary>
[TestFixture]
public sealed class MultiCharacterTest : GameTest
{
    private static readonly ProtoId<JobPrototype> Passenger = "Passenger";
    private static readonly ProtoId<JobPrototype> Engineer = "StationEngineer";
    private static readonly ProtoId<JobPrototype> Captain = "Captain";
    private static readonly ProtoId<JobPrototype> AgeGated = "MoffMultiCharacterAgeGatedJob";

    private const string Map = "MoffMultiCharacterTestMap";

    private const string SlotZeroName = "Slot Zero Guy";
    private const string SlotOneName = "Slot One Guy";

    [TestPrototypes]
    private static readonly string TestMap = @$"
- type: job
  id: {AgeGated}
  name: job-name-passenger
  description: job-description-passenger
  playTimeTracker: JobPassenger
  startingGear: PassengerGear
  icon: ""JobIconPassenger""
  supervisors: job-supervisors-everyone
  requirements:
  - !type:AgeRequirement
    requiredAge: 30

- type: gameMap
  id: {Map}
  mapName: {Map}
  mapPath: /Maps/Test/empty.yml
  minPlayers: 0
  stations:
    Empty:
      stationProto: StandardNanotrasenStation
      components:
        - type: StationNameSetup
          mapNameTemplate: ""Empty""
        - type: StationJobs
          availableJobs:
            {Passenger}: [ -1, -1 ]
            {Engineer}: [ -1, -1 ]
            {Captain}: [ 1, 1 ]
            {AgeGated}: [ -1, -1 ]
";

    public override PoolSettings PoolSettings => new()
    {
        DummyTicker = false,
        Connected = true,
        InLobby = true,
    };

    /// <summary>
    /// Two characters taking one job each, named so the spawned entity identifies the winner.
    /// </summary>
    private async Task SetupTwoCharacters(
        TestPair pair,
        ProtoId<JobPrototype> slotZeroJob,
        ProtoId<JobPrototype> slotOneJob)
    {
        var prefMan = pair.Server.ResolveDependency<IServerPreferencesManager>();
        var user = pair.Client.User!.Value;

        var slotZero = new HumanoidCharacterProfile()
            .WithName(SlotZeroName)
            .WithJobPriorities(new Dictionary<ProtoId<JobPrototype>, JobPriority>
            {
                [slotZeroJob] = JobPriority.Medium,
            });

        var slotOne = new HumanoidCharacterProfile()
            .WithName(SlotOneName)
            .WithJobPriorities(new Dictionary<ProtoId<JobPrototype>, JobPriority>
            {
                [slotOneJob] = JobPriority.Medium,
            });

        await pair.Server.WaitPost(() => prefMan.SetProfile(user, 0, slotZero).Wait());
        await pair.Server.WaitPost(() => prefMan.SetProfile(user, 1, slotOne).Wait());
    }

    private async Task SetGlobalPriorities(
        TestPair pair,
        params (ProtoId<JobPrototype> Job, JobPriority Priority)[] priorities)
    {
        var selection = pair.Server.ResolveDependency<MoffCharacterSelectionManager>();
        var user = pair.Client.User!.Value;

        var dict = new Dictionary<ProtoId<JobPrototype>, JobPriority>();
        foreach (var (job, priority) in priorities)
        {
            dict[job] = priority;
        }

        await pair.Server.WaitPost(() => selection.SetJobPriorities(user, dict).Wait());
    }

    private async Task SetSlotEnabled(TestPair pair, int slot, bool enabled)
    {
        var selection = pair.Server.ResolveDependency<MoffCharacterSelectionManager>();
        var user = pair.Client.User!.Value;

        await pair.Server.WaitPost(() =>
        {
            // GetState hands back a throwaway default when nothing is cached.
            Assert.That(selection.TryGetState(user, out var state),
                Is.True,
                "Selection state was not loaded for the test user.");

            state!.EnabledSlots[slot] = enabled;
        });
    }

    /// <summary>
    /// Automatic preference resetting only covers slot 0, so slot 1 would leak between tests.
    /// </summary>
    [TearDown]
    public async Task CleanupSecondCharacter()
    {
        var pair = Pair;
        var prefMan = pair.Server.ResolveDependency<IServerPreferencesManager>();
        var selection = pair.Server.ResolveDependency<MoffCharacterSelectionManager>();
        var user = pair.Client.User!.Value;

        await pair.Server.WaitPost(() => prefMan.SetProfile(user, 1, new HumanoidCharacterProfile()).Wait());

        // Deactivating is what isolates: an inactive slot contributes no candidates.
        await pair.Server.WaitPost(() =>
        {
            if (selection.TryGetState(user, out var state))
            {
                state.EnabledSlots[1] = false;
                state.JobPriorities.Clear();
            }
        });
    }

    private async Task StartRound(TestPair pair)
    {
        var ticker = pair.Server.System<GameTicker>();
        ticker.ToggleReadyAll(true);
        await pair.Server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(10);
    }

    /// <summary>Asserts the assigned job and which character spawned.</summary>
    private void AssertJobAndCharacter(TestPair pair, ProtoId<JobPrototype> job, string characterName)
    {
        var jobSys = pair.Server.System<SharedJobSystem>();
        var mindSys = pair.Server.System<MindSystem>();
        var ticker = pair.Server.System<GameTicker>();
        var user = pair.Client.User!.Value;

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        Assert.That(ticker.PlayerGameStatuses[user], Is.EqualTo(PlayerGameStatus.JoinedGame));

        var uid = pair.Server.PlayerMan.SessionsDict.GetValueOrDefault(user)?.AttachedEntity;
        Assert.That(pair.Server.EntMan.EntityExists(uid));

        var mind = mindSys.GetMind(uid!.Value);
        Assert.That(jobSys.MindTryGetJobId(mind, out var actualJob));
        Assert.That(actualJob, Is.EqualTo(job));

        Assert.That(pair.Server.EntMan.GetComponent<MetaDataComponent>(uid.Value).EntityName,
            Is.EqualTo(characterName),
            "The wrong character was spawned for the assigned job.");
    }

    /// <summary>
    /// A job on a non-selected character still makes the player eligible, and that one spawns.
    /// </summary>
    [Test]
    public async Task SpawnsCharacterMatchingAssignedJob()
    {
        var pair = Pair;
        pair.Server.CfgMan.SetCVar(CCVars.GameMap, Map);
        var ticker = pair.Server.System<GameTicker>();

        await SetupTwoCharacters(pair, Passenger, Engineer);

        // Only Engineer has a priority, and only slot 1 will take it.
        await SetGlobalPriorities(pair, (Engineer, JobPriority.High));

        await StartRound(pair);

        AssertJobAndCharacter(pair, Engineer, SlotOneName);

        await pair.Server.WaitPost(() => ticker.RestartRound());
    }

    /// <summary>Inactive characters are skipped even when selected and job-eligible.</summary>
    [Test]
    public async Task InactiveCharacterIsNotSpawned()
    {
        var pair = Pair;
        pair.Server.CfgMan.SetCVar(CCVars.GameMap, Map);
        var ticker = pair.Server.System<GameTicker>();

        await SetupTwoCharacters(pair, Engineer, Engineer);

        // Slot 0 is selected and wants Engineer, but is inactive.
        await SetGlobalPriorities(pair, (Engineer, JobPriority.High));
        await SetSlotEnabled(pair, 0, false);

        await StartRound(pair);

        AssertJobAndCharacter(pair, Engineer, SlotOneName);

        await pair.Server.WaitPost(() => ticker.RestartRound());
    }

    /// <summary>
    /// An inactive character contributes no candidate jobs, even when it is the selected one. If it
    /// did, the player would be assigned a job no active character can fill.
    /// </summary>
    [Test]
    public async Task InactiveSelectedCharacterContributesNoJobs()
    {
        var pair = Pair;
        pair.Server.CfgMan.SetCVar(CCVars.GameMap, Map);
        var ticker = pair.Server.System<GameTicker>();

        // Slot 0 is the selected character and the only one wanting Captain.
        await SetupTwoCharacters(pair, Captain, Passenger);
        await SetGlobalPriorities(pair, (Captain, JobPriority.High), (Passenger, JobPriority.Medium));
        await SetSlotEnabled(pair, 0, false);

        await StartRound(pair);

        AssertJobAndCharacter(pair, Passenger, SlotOneName);

        await pair.Server.WaitPost(() => ticker.RestartRound());
    }

    /// <summary>
    /// Jobs contributed by a non-selected character are still subject to role timers, and a player
    /// who fails them falls back to a job they can hold rather than not spawning at all.
    /// </summary>
    [Test]
    public async Task RoleTimersApplyToOtherCharactersJobs()
    {
        var pair = Pair;
        pair.Server.CfgMan.SetCVar(CCVars.GameMap, Map);
        var ticker = pair.Server.System<GameTicker>();

        await OverrideCVar(Side.Server, CCVars.GameRoleTimers, true);

        await SetupTwoCharacters(pair, Passenger, Captain);
        await SetGlobalPriorities(pair, (Captain, JobPriority.High), (Passenger, JobPriority.Medium));

        await StartRound(pair);

        // The test user has no playtime, so Captain is filtered out before jobs are assigned.
        AssertJobAndCharacter(pair, Passenger, SlotZeroName);

        await pair.Server.WaitPost(() => ticker.RestartRound());
    }

    /// <summary>High outranks Medium even across different characters.</summary>
    [Test]
    public async Task GlobalPriorityAppliesAcrossCharacters()
    {
        var pair = Pair;
        pair.Server.CfgMan.SetCVar(CCVars.GameMap, Map);
        var ticker = pair.Server.System<GameTicker>();

        await SetupTwoCharacters(pair, Passenger, Captain);

        await SetGlobalPriorities(pair, (Passenger, JobPriority.Medium), (Captain, JobPriority.High));

        await StartRound(pair);

        AssertJobAndCharacter(pair, Captain, SlotOneName);

        await pair.Server.WaitPost(() => ticker.RestartRound());

        // Flip the preference; the other character should win.
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        await SetGlobalPriorities(pair, (Passenger, JobPriority.High), (Captain, JobPriority.Medium));

        await StartRound(pair);

        AssertJobAndCharacter(pair, Passenger, SlotZeroName);

        await pair.Server.WaitPost(() => ticker.RestartRound());
    }

    /// <summary>
    /// Which character fills a job still has to respect the job's age and species gates when role
    /// timers are off, or the pick would ignore them entirely.
    /// </summary>
    [Test]
    public async Task AgeRequirementAppliesWithRoleTimersOff()
    {
        var pair = Pair;
        pair.Server.CfgMan.SetCVar(CCVars.GameMap, Map);
        var ticker = pair.Server.System<GameTicker>();

        await OverrideCVar(Side.Server, CCVars.GameRoleTimers, false);

        var prefMan = pair.Server.ResolveDependency<IServerPreferencesManager>();
        var user = pair.Client.User!.Value;

        // Both characters want the same age gated job; only the older one may hold it.
        var young = new HumanoidCharacterProfile()
            .WithName(SlotZeroName)
            .WithAge(20)
            .WithJobPriorities(new Dictionary<ProtoId<JobPrototype>, JobPriority>
            {
                [AgeGated] = JobPriority.Medium,
            });

        var old = new HumanoidCharacterProfile()
            .WithName(SlotOneName)
            .WithAge(60)
            .WithJobPriorities(new Dictionary<ProtoId<JobPrototype>, JobPriority>
            {
                [AgeGated] = JobPriority.Medium,
            });

        await pair.Server.WaitPost(() => prefMan.SetProfile(user, 0, young).Wait());
        await pair.Server.WaitPost(() => prefMan.SetProfile(user, 1, old).Wait());

        await SetGlobalPriorities(pair, (AgeGated, JobPriority.High));

        await StartRound(pair);

        AssertJobAndCharacter(pair, AgeGated, SlotOneName);

        await pair.Server.WaitPost(() => ticker.RestartRound());
    }
}
