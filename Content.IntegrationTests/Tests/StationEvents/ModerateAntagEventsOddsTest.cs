#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.StationEvents;
using Content.Shared.EntityTable;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.StationEvents;

/// <summary>
/// Prints, for every entry in <see cref="TableId"/>, the true odds of it being the event selected at
/// <see cref="RoundDurationAtCheck"/> hours into the round, across a grid of player counts. This is a
/// diagnostic tool for comparing relative event frequency, not a correctness test.
/// </summary>
public sealed class ModerateAntagEventsOddsTest : GameTest
{
    // How far into the round to evaluate odds at. Change this to benchmark a different point in the round.
    private static readonly TimeSpan RoundDurationAtCheck = TimeSpan.FromHours(1f);

    private const int MinPlayers = 5;
    private const int MaxPlayers = 75;
    private const int PlayerStep = 5;

    private const string TableId = "ModerateAntagEventsTable";

    // DerelictBorgEventTable's individual entries, collapsed into one aggregate row since it appears as a single
    // entry in ModerateAntagEventsTable.
    private const string DerelictBorgRowName = "DerelictBorgEventTable";

    private static readonly HashSet<string> DerelictBorgSpawnIds =
    [
        "DerelictEngineerCyborgSpawn",
        "DerelictGenericCyborgSpawn",
        "DerelictJanitorCyborgSpawn",
        "DerelictMedicalCyborgSpawn",
        "DerelictMiningCyborgSpawn",
        "DerelictServiceCyborgSpawn",
        "DerelictSyndicateAssaultCyborgSpawn",
    ];

    public override PoolSettings PoolSettings => new()
    {
        Connected = false,
    };

    [SidedDependency(Side.Server)] private readonly IPrototypeManager _sProtoMan = default!;
    [SidedDependency(Side.Server)] private readonly EntityTableSystem _sEntityTable = default!;
    [SidedDependency(Side.Server)] private readonly EventManagerSystem _sEventManager = default!;

    [Test]
    public async Task PrintOdds()
    {
        var pair = Pair;
        var server = pair.Server;

        // playerCount -> (rowName -> probability)
        var columns = new List<(int playerCount, Dictionary<string, float> probsByRow)>();
        var rowNames = new List<string>();

        await server.WaitAssertion(() =>
        {
            var tableProto = _sProtoMan.Index<EntityTablePrototype>(TableId);
            var flattened = _sEntityTable.ListSpawns(tableProto).ToList();

            for (var playerCount = MinPlayers; playerCount <= MaxPlayers; playerCount += PlayerStep)
            {
                var probsByRow = new Dictionary<string, float>();

                foreach (var (protoId, prob) in _sEventManager.ListLimitedEvents(flattened,
                             RoundDurationAtCheck,
                             playerCount))
                {
                    var rowName = DerelictBorgSpawnIds.Contains(protoId.Id) ? DerelictBorgRowName : protoId.Id;
                    probsByRow.TryGetValue(rowName, out var existing);
                    probsByRow[rowName] = existing + prob;
                }

                columns.Add((playerCount, probsByRow));
            }

            rowNames = columns
                .SelectMany(c => c.probsByRow.Keys)
                .Distinct()
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
        });

        Assert.That(rowNames, Is.Not.Empty, $"No rows were produced for '{TableId}' - is the table id correct?");

        const string rowHeader = "Event";
        var nameColumnWidth = Math.Max(rowHeader.Length, rowNames.Max(n => n.Length));

        var sb = new StringBuilder();
        sb.Append(rowHeader.PadRight(nameColumnWidth));
        foreach (var (playerCount, _) in columns)
        {
            sb.Append(' ').Append(playerCount.ToString().PadLeft(6));
        }

        Console.WriteLine(sb.ToString());

        foreach (var rowName in rowNames)
        {
            sb.Clear();
            sb.Append(rowName.PadRight(nameColumnWidth));
            foreach (var (_, probsByRow) in columns)
            {
                var columnTotal = probsByRow.Values.Sum();
                probsByRow.TryGetValue(rowName, out var prob);
                var trueProb = columnTotal > 0f ? prob / columnTotal : 0f;
                sb.Append(' ').Append($"{trueProb * 100f:0.0}%".PadLeft(6));
            }

            Console.WriteLine(sb.ToString());
        }
    }
}
