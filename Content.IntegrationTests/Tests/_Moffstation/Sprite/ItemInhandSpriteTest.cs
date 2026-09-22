#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.IntegrationTests.Tests._Moffstation.Sprite;

[TestFixture]
[TestOf(typeof(ItemComponent))]
public sealed class ItemInhandSpriteTest : GameTest
{
    private static readonly HashSet<string> Ignored =
    [
        "VirtualItem",
    ];

    [SidedDependency(Side.Client)] private readonly IResourceCache _resCache = default!;

    [Test]
    [RunOnSide(Side.Client)]
    [Description("Complains if items exist with inhand sprites defined but missing from assets")]
    public void ItemInhandSpritesExist()
    {
        List<Failure> errors = [];

        foreach (var protoKey in GameDataScrounger.EntitiesWithComponent("Item"))
        {
            if (Ignored.Contains(protoKey))
                continue;

            var uid = CSpawn(protoKey);

            try
            {
                CEntMan.RunMapInit(uid, CComp<MetaDataComponent>(uid));

                var item = CComp<ItemComponent>(uid);
                CTryComp<SpriteComponent>(uid, out var sprite);

                foreach (var (location, layers) in item.InhandVisuals)
                {
                    foreach (var (layerIndex, layer) in layers.Index())
                    {
                        if (layer.State == null)
                            continue;

                        RSI? rsi;
                        if (layer.RsiPath != null)
                        {
                            rsi = _resCache
                                .GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / layer.RsiPath)
                                .RSI;
                        }
                        else if (item.RsiPath != null)
                        {
                            rsi = _resCache
                                .GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / item.RsiPath)
                                .RSI;
                        }
                        else
                            rsi = sprite?.BaseRSI;

                        if (rsi == null)
                        {
                            errors.Add(new Failure(protoKey, location, layerIndex, layer.State, null));
                            continue;
                        }

                        if (!rsi.TryGetState(layer.State, out _))
                        {
                            errors.Add(new Failure(protoKey, location, layerIndex, layer.State, rsi));
                        }
                    }
                }
            }
            finally
            {
                CDeleteNow(uid);
            }
        }

        using (Assert.EnterMultipleScope())
        {
            foreach (var failure in errors)
            {
                Assert.Fail(failure.ToString());
            }
        }
    }

    private readonly partial record struct Failure(
        EntProtoId Entity,
        HandLocation Hand,
        int Layer,
        string RsiState,
        RSI? Rsi)
    {
        public override string ToString() => Rsi != null
            ? $"{Entity}'s {Hand} hand visuals layer #{Layer} references state \"{RsiState}\" which does not exist in {Rsi.Path}"
            : $"{Entity}'s {Hand} hand visuals layer #{Layer} do not have an RSI";
    }
}
