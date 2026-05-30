using System.Linq;
using Content.Server.Cargo.Systems;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Moffstation;

[TestFixture]
public sealed class PirateTest
{
    private static readonly string[] PirateMaps =
    [
        "/Maps/_Moffstation/Nonstations/pirate-cove.yml",
        "/Maps/_Moffstation/Shuttles/shuttle-sp-scurvydog.yml",
        "/Maps/_Moffstation/Shuttles/shuttle-sp-sparrow.yml",
    ];

    private static readonly ProtoId<CargoBountyGroupPrototype> PirateBountyGroup = "Pirates";

    [Test]
    public async Task CheckNoBountyEntriesOnPirateMaps()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entManager = server.ResolveDependency<IEntityManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var loader = server.System<MapLoaderSystem>();
        var cargoSystem = server.System<CargoSystem>();

        var pirateBountyEntries = protoManager.EnumeratePrototypes<CargoBountyPrototype>()
            .Where(b => b.Group == PirateBountyGroup)
            .SelectMany(
                proto => proto.Entries,
                (proto, entry) => (proto.ID, entry)
            )
            .ToList();

        await server.WaitPost(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var path in PirateMaps)
                {
                    mapSystem.CreateMap(out var mapId);
                    if (!loader.TryLoadGrid(mapId, new ResPath(path), out _))
                    {
                        Assert.Fail($"File {path} contains several maps!");
                    }

                    var entityQuery = entManager.EntityQueryEnumerator<TransformComponent>();
                    while (entityQuery.MoveNext(out var entUid, out var transform))
                    {
                        if (transform.MapID != mapId)
                            continue;
                        foreach (var (id, bountyItemEntry) in pirateBountyEntries)
                        {
                            var entityPrototypeId = entManager.GetComponentOrNull<MetaDataComponent>(entUid)?.EntityPrototype?.ID;
                            Assert.That(
                                !cargoSystem.IsValidBountyEntry(entUid, bountyItemEntry),
                                $"Entity {entUid} with proto={entityPrototypeId} on map {path} fulfills {Loc.GetString(bountyItemEntry.Name)} for pirate bounty {id}"
                            );
                        }
                    }
                    mapSystem.DeleteMap(mapId);
                }
            });
        });
        await server.WaitRunTicks(1);
        await pair.CleanReturnAsync();
    }
}
