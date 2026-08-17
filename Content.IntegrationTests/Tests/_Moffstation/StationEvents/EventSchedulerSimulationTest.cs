using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Server._Moffstation.StationEvents;
using Content.Server.StationEvents;
using Content.Shared.EntityTable;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests._Moffstation.StationEvents;

/// <summary>
/// Simulates the Moffstation event schedulers over many rounds and prints how they pace a round, so their timings can
/// be tuned without playing forty of them. The assertions only check that the state machine isn't broken - the numbers
/// themselves are reported, not asserted, so RNG can't make this flaky.
/// </summary>
[TestFixture]
public sealed class EventSchedulerSimulationTest : GameTest
{
    private const int Seed = 1337;
    private const int Rounds = 1000;
    private const int PlayerCount = 50;
    private const float RoundLengthMean = 90f;
    private const float RoundLengthStdDev = 10f;

    private static readonly EntProtoId[] Schedulers =
    [
        "MoffCalmStationEventScheduler",
        "MoffSpicyStationEventScheduler",
    ];

    // The simulation reseeds the shared RNG, so this pair can't be reused.
    public override PoolSettings PoolSettings => new() { Dirty = true };

    [Test]
    public async Task SimulateSchedulers()
    {
        var server = Pair.Server;
        await server.WaitIdleAsync();

        var results = new List<SimulationResult>();

        await server.WaitPost(() =>
        {
            var random = server.ResolveDependency<IRobustRandom>();
            var sysMan = server.ResolveDependency<IEntitySystemManager>();
            random.SetSeed(Seed);

            var simulator = new EventSchedulerSimulator(
                server.ResolveDependency<IPrototypeManager>(),
                server.ResolveDependency<IComponentFactory>(),
                random,
                sysMan.GetEntitySystem<EventManagerSystem>(),
                sysMan.GetEntitySystem<EntityTableSystem>(),
                sysMan.GetEntitySystem<MoffStationEventSchedulerSystem>());

            foreach (var scheduler in Schedulers)
            {
                results.Add(simulator.Simulate(scheduler, Rounds, PlayerCount, RoundLengthMean, RoundLengthStdDev));
            }
        });

        foreach (var result in results)
        {
            TestContext.Out.WriteLine(result.FormatTable());
        }

        Assert.Multiple(() =>
        {
            foreach (var result in results)
            {
                Assert.That(result.EventsPerRound, Is.GreaterThan(0f), $"{result.Scheduler} ran no events at all.");

                foreach (var (stateId, stats) in result.States)
                {
                    Assert.That(stats.Visits,
                        Is.GreaterThan(0),
                        $"{result.Scheduler} never entered state \"{stateId}\".");

                    if (stats.FiresEvents)
                    {
                        Assert.That(stats.Events,
                            Is.GreaterThan(0),
                            $"{result.Scheduler} ran no events in state \"{stateId}\".");
                    }
                    else
                    {
                        Assert.That(stats.Events,
                            Is.Zero,
                            $"{result.Scheduler} ran events in state \"{stateId}\", which has no event timing.");
                    }
                }
            }
        });
    }
}
