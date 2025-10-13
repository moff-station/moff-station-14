using Content.Server._Offbrand.VariationPass;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.VariationPass;
using Content.Shared.Lathe;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.VariationPass.Offmed;

// MOFFSTATION - most of this file has been rewritten
/// <inheritdoc cref="SupplyNearLatheVariationPassComponent"/>
public sealed class SupplyNearLatheVariationPassSystem : VariationPassSystem<SupplyNearLatheVariationPassComponent>
{
    private HashSet<EntityUid> FindLathesOnStation(EntProtoId proto, EntityUid station)
    {
        var lathes = new HashSet<EntityUid>();
        var query = AllEntityQuery<LatheComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (MetaData(uid).EntityPrototype?.ID is { } existingProto && existingProto == proto)
                lathes.Add(uid);
        }
        return lathes;
    }

    protected override void ApplyVariation(Entity<SupplyNearLatheVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        if (FindLathesOnStation(ent.Comp.LathePrototype, args.Station) is not { } lathes)
            return;

        foreach (var lathe in lathes)
        {
            SpawnNextToOrDrop(ent.Comp.EntityToSpawn, lathe);
        }
    }
}
