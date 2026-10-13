//using Content.Server.Silicons.Laws; // macro remove
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Station.Components;
using Content.Shared._Moffstation.Traits.Components;
using Content.Shared._Moffstation.Traits.EntitySystems;
using Robust.Shared.Random; // macro

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events that alter the laws of silicon entities (e.g. cyborgs, AI) on the affected station.
/// </summary>
/// <seealso cref="IonStormRuleComponent"/>
public sealed partial class IonStormRule : StationEventSystem<IonStormRuleComponent>
{
    [Dependency] private SharedEmpVulnerableSystem _empVulnerable = default!; //Moffstation - EMP Vulnerability
    [Dependency] private IRobustRandom _random = default!; // macro

    protected override void Started(Entity<IonStormRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        if (!Station.TryGetRandomStation<StationEventEligibleComponent>(out var chosenStation))
            return;

        // begin macro edit
        // var query = EntityQueryEnumerator<SiliconLawBoundComponent, TransformComponent, IonStormTargetComponent>();
        var query = EntityQueryEnumerator<IonStormTargetComponent, TransformComponent>();
        while (query.MoveNext(out var _, out var target, out var xform))
        // end macro edit
        {
            // only affect law holders on the station, and check random chance (macro edit)
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation.Value ||
                !_random.Prob(target.Chance)) // macro
                continue;
            // begin macro edit again
            var ev = new IonStormEvent();
            RaiseLocalEvent(ent, ref ev);
            //     _ionStorm.IonStormTarget((ent, lawBound, target)); // end macro
        }

        //Moffstation - Begin - EMP Vulnerability
        var empAffectedQuery = EntityQueryEnumerator<EmpVulnerableComponent, TransformComponent>();
        while (empAffectedQuery.MoveNext(out var uid, out var empVulnerable, out var xform))
        {
            // only affect vulnerable entities on the station
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation.Value.Owner)
                continue;

            _empVulnerable.IonStormTarget((uid, empVulnerable));
        }
        //Moffstation - End
    }
}
// macro add
/// <summary>
/// Event raised on an entity with <see cref="IonStormTargetComponent"/> when an ion storm occurs on the attached station.
/// </summary>
[ByRefEvent]
public record struct IonStormEvent(bool Adminlog = true);
