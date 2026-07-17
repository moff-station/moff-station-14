using Content.Server._Moffstation.Silicons.Laws;
using Content.Server._Moffstation.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._Moffstation.Traits.EntitySystems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.StationEvents.Events;

/// <summary>
/// Handles <see cref="LawBreakerVirusRuleComponent"/>. This rule act as a cyber-attack targeting the lawsets of  silicons.
/// When started, the event will generate a lawset at random. All Ion-stormable, law-bound entities of the station
/// will follow this lawset.
/// </summary>
public sealed partial class LawBreakerVirusRule : StationEventSystem<LawBreakerVirusRuleComponent>
{
    [Dependency] private IonAttackSystem _ion =  default!;

    protected override void Started(EntityUid uid,
        LawBreakerVirusRuleComponent comp,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        var ionLawset = _ion.GenerateLawset((uid, comp));

        var query = EntityQueryEnumerator<IonStormTargetComponent, TransformComponent>();
        var ev = new LawBreakerVirusEvent(ionLawset);
        while (query.MoveNext(out var ent, out _, out var xform))
        {
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation)
                continue;
            RaiseLocalEvent(ent, ref ev);
        }
    }
}

/// <summary>
/// Event raised on an entity with <see cref="IonStormTargetComponent"/> when an ion storm occurs on the attached station.
/// </summary>
[ByRefEvent]
public record struct LawBreakerVirusEvent(SiliconLawset Lawset);
