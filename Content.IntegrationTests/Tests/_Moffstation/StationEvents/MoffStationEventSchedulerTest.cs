using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Server._Moffstation.StationEvents;
using Content.Server._Moffstation.StationEvents.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Moffstation.StationEvents;

[TestOf(typeof(MoffStationEventSchedulerComponent))]
[TestOf(typeof(MoffStationEventSchedulerSystem))]
public sealed class MoffStationEventSchedulerTest : GameTest
{
    private static readonly string[] Schedulers = GameDataScrounger.EntitiesWithComponent("MoffStationEventScheduler");

    [SidedDependency(Side.Server)] private readonly IComponentFactory _compFactory = null!;

    [Test]
    [TestCaseSource(nameof(Schedulers))]
    [Description("Ensures all Moffstation Event schedulers are wired properly")]
    public async Task StateGraphIsComplete(string schedulerId)
    {
        await Server.WaitAssertion(() =>
        {
            var proto = SProtoMan.Index<EntityPrototype>(schedulerId);
            Assume.That(proto.TryComp<MoffStationEventSchedulerComponent>(out var scheduler, _compFactory), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(scheduler!.States.ContainsKey(scheduler.InitialState),
                    $"{schedulerId} starts in state undefined state: \"{scheduler.InitialState}\".");

                foreach (var (id, state) in scheduler.States)
                {
                    foreach (var next in state.NextStates.Keys)
                    {
                        Assert.That(scheduler.States.ContainsKey(next),
                            $"{schedulerId} state \"{id}\" points at undefined state: \"{next}\"");
                    }
                }
            }
        });
    }
}
