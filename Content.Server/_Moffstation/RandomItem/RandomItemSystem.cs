using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Hands.Systems;
using Content.Shared.Item;
using Content.Shared.Whitelist;
using Robust.Server.Audio;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.RandomItem;

/// <summary>
/// This handles...
/// </summary>
public sealed class RandomItemSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private readonly List<EntProtoId> _possibleGiftsSafe = new();
    private readonly List<EntProtoId> _possibleGiftsUnsafe = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<RandomItemComponent, MapInitEvent>(OnMapInit);

        BuildIndex();
    }

    private void OnMapInit(Entity<RandomItemComponent> ent, ref MapInitEvent args)
    {
        var validSpawns = (ent.Comp.InsaneMode ? _possibleGiftsUnsafe : _possibleGiftsSafe)
            // .Where(spawn => ent.Comp.Whitelist!.Intersect(_prototype.Index(spawn).Components.Keys).Any())
            // .Where(spawn => !ent.Comp.Blacklist!.Intersect(_prototype.Index(spawn).Components.Keys).Any())
            .ToHashSet();
        Spawn(_random.Pick(validSpawns),  Transform(ent.Owner).Coordinates);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        if (obj.WasModified<EntityPrototype>())
            BuildIndex();
    }

    private void BuildIndex()
    {
        _possibleGiftsSafe.Clear();
        _possibleGiftsUnsafe.Clear();
        var itemCompName = Factory.GetComponentName<ItemComponent>();
        var mapGridCompName = Factory.GetComponentName<MapGridComponent>();
        var physicsCompName = Factory.GetComponentName<PhysicsComponent>();

        foreach (var proto in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract || proto.HideSpawnMenu || proto.Components.ContainsKey(mapGridCompName) || !proto.Components.ContainsKey(physicsCompName))
                continue;

            _possibleGiftsUnsafe.Add(proto.ID);

            if (!proto.Components.ContainsKey(itemCompName))
                continue;

            _possibleGiftsSafe.Add(proto.ID);
        }
    }
}
