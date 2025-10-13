//
//  Moffstation - This file has been rewritten and moved to our namespace
//  We added support for it being able to spawn the crate on multiple lathes
//  Its not a huge change or anything, but I had to namespace most stuff in the functions so I might as well
//  Start using this one again if multi-lathe support is added
//
/*
using Content.Server.GameTicking.Rules.VariationPass;
using Content.Server.GameTicking.Rules;
using Content.Shared.Examine;
using Content.Shared.Lathe;
using Robust.Shared.Prototypes;

namespace Content.Server._Offbrand.VariationPass;

/// <inheritdoc cref="SupplyNearLatheVariationPassComponent"/>
public sealed class SupplyNearLatheVariationPassSystem : VariationPassSystem<SupplyNearLatheVariationPassComponent>
{
    private EntityUid? FindLatheOnStation(EntProtoId proto, EntityUid station)
    {
        var query = AllEntityQuery<LatheComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (MetaData(uid).EntityPrototype?.ID is { } existingProto && existingProto == proto)
                return uid;
        }
        return null;
    }

    protected override void ApplyVariation(Entity<SupplyNearLatheVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        if (FindLatheOnStation(ent.Comp.LathePrototype, args.Station) is not { } lathe)
            return;

        SpawnNextToOrDrop(ent.Comp.EntityToSpawn, lathe);
    }
}
*/
