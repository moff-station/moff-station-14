using Content.Server.Silicons.Laws;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Station.Components;
//Moffstation - EMP Vulnerability - Begin
using Content.Shared._Moffstation.Traits.Components;
using Content.Shared._Moffstation.Traits.EntitySystems;
//Moffstation - End

namespace Content.Server.StationEvents.Events;

public sealed class IonStormRule : StationEventSystem<IonStormRuleComponent>
{
    [Dependency] private readonly IonStormSystem _ionStorm = default!;
    [Dependency] private readonly SharedEmpVulnerableSystem _empVulnerable = default!; //Moffstation - EMP Vulnerability

    protected override void Started(EntityUid uid, IonStormRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        var query = EntityQueryEnumerator<SiliconLawBoundComponent, TransformComponent, IonStormTargetComponent>();
        while (query.MoveNext(out var ent, out var lawBound, out var xform, out var target))
        {
            // only affect law holders on the station
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation)
                continue;

            _ionStorm.IonStormTarget((ent, lawBound, target));
        }

        //Moffstation - Begin - EMP Vulnerability
        var empAffectedQuery = EntityQueryEnumerator<EmpVulnerableComponent, TransformComponent>();
        while (empAffectedQuery.MoveNext(out var ent, out var empVulnerable, out var xform))
        {
            // only affect vulnerable entities on the station
            if(CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation)
                continue;

            _empVulnerable.IonStormTarget((ent, empVulnerable));
        }
        //Moffstation - End
    }
}
