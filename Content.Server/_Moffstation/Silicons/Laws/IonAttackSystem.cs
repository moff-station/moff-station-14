using System.Linq;
using Content.Server._Moffstation.StationEvents.Components;
using Content.Server._Moffstation.StationEvents.Events;
using Content.Server.Silicons.Laws;
using Content.Shared.Random.Helpers;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.Silicons.Laws;

public sealed partial class LawBreakerVirusSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SiliconLawSystem _laws = default!;
    [Dependency] private IonLawSystem _ionLaw = default!;


    [SubscribeLocalEvent]
    private void OnIonAttack(Entity<IonStormTargetComponent> ent, ref LawBreakerVirusEvent args)
    {
        var ev = new IonStormLawsEvent(args.Lawset);
        RaiseLocalEvent(ent, ref ev);
    }

    public SiliconLawset GenerateLawset(Entity<LawBreakerVirusRuleComponent> ent)
    {
        var laws = _laws.GetLawset(ProtoMan.Index(ent.Comp.StartingLawset).Pick(_random));

        if (_random.Prob(ent.Comp.ShuffleChance))
        {
            laws.Laws = laws.Laws.OrderBy(x => _random.Next()).ToList();
        }

        if (laws.Laws.Count > 0 && _random.Prob(ent.Comp.RemoveChance))
        {
            var i = _random.Next(laws.Laws.Count);
            laws.Laws.RemoveAt(i);
        }

        if (laws.Laws.Count > 0 && _random.Prob(ent.Comp.ReplaceChance))
        {
            var newLaw = _ionLaw.GetIonLaw();
            var i = _random.Next(laws.Laws.Count);
            laws.Laws[i] = new SiliconLaw
            {
                LawString = newLaw,
                Order = laws.Laws[i].Order,
            };
        }

        return laws;
    }
}
