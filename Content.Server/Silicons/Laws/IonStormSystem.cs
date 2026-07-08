using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Server.StationEvents.Events; // macro
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Silicons.Laws;

public sealed partial class IonStormSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SiliconLawSystem _siliconLaw = default!;
    [Dependency] private IRobustRandom _robustRandom = default!;
    [Dependency] private IonLawSystem _ionLaw = default!;

    // macro add start
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IonStormTargetComponent, IonStormEvent>(IonStormTarget);
    }
    // macro add end

    /// <summary>
    /// Randomly alters the laws of an individual silicon.
    /// </summary>
    public void IonStormTarget(Entity<IonStormTargetComponent> ent, ref IonStormEvent args) // macro edit, its an event subscription now
    {
        // if (!_robustRandom.Prob(target.Chance)) // macro, moved to ionstormrule
        //     return;
        // end macro

        var laws = _siliconLaw.GetProviderLaws(ent.Owner);
        if (laws.Laws.Count == 0)
            return;

        // try to swap it out with a random lawset
        if (_robustRandom.Prob(ent.Comp.RandomLawsetChance)) // Moffstation - make use of the component
        {
            var lawsets = _proto.Index(ent.Comp.RandomLawsets); // Moffstation - make use of the component
            var lawset = lawsets.Pick(_robustRandom);
            laws = _siliconLaw.GetLawset(lawset);
        }
        // clone it so not modifying stations lawset
        laws = laws.Clone();

        // shuffle them all
        if (_robustRandom.Prob(ent.Comp.ShuffleChance)) // Moffstation - make use of the component
        {
            // hopefully work with existing glitched laws if there are multiple ion storms
            var baseOrder = FixedPoint2.New(1);
            foreach (var law in laws.Laws)
            {
                if (law.Order < baseOrder)
                    baseOrder = law.Order;
            }

            _robustRandom.Shuffle(laws.Laws);

            // change order based on shuffled position
            for (int i = 0; i < laws.Laws.Count; i++)
            {
                laws.Laws[i].Order = baseOrder + i;
            }
        }

        // see if we can remove a random law
        if (laws.Laws.Count > 0 && _robustRandom.Prob(ent.Comp.RemoveChance)) // Moffstation - make use of the component
        {
            var i = _robustRandom.Next(laws.Laws.Count);
            laws.Laws.RemoveAt(i);
        }

        // generate a new law...
        var newLaw = _ionLaw.GetIonLaw();

        if (string.IsNullOrEmpty(newLaw))
            return;

        // see if the law we add will replace a random existing law or be a new glitched order one
        if (laws.Laws.Count > 0 && _robustRandom.Prob(ent.Comp.ReplaceChance))
        {
            var i = _robustRandom.Next(laws.Laws.Count);
            laws.Laws[i] = new SiliconLaw()
            {
                LawString = newLaw,
                Order = laws.Laws[i].Order
            };
        }
        else
        {
            laws.Laws.Insert(0, new SiliconLaw
            {
                LawString = newLaw,
                Order = -1,
                LawIdentifierOverride = Loc.GetString("ion-storm-law-scrambled-number", ("length", _robustRandom.Next(5, 10)))
            });
        }

        // sets all unobfuscated laws' indentifier in order from highest to lowest priority
        // This could technically override the Obfuscation from the code above, but it seems unlikely enough to basically never happen
        int orderDeduction = -1;

        for (int i = 0; i < laws.Laws.Count; i++)
        {
            var notNullIdentifier = laws.Laws[i].LawIdentifierOverride ?? (i - orderDeduction).ToString();

            if (notNullIdentifier.Any(char.IsSymbol))
            {
                orderDeduction += 1;
            }
            else
            {
                laws.Laws[i].LawIdentifierOverride = (i - orderDeduction).ToString();
            }
        }

        // adminlog is used to prevent adminlog spam.
        if (args.Adminlog) //macro edit
            _adminLogger.Add(LogType.Mind, LogImpact.High, $"{ToPrettyString(ent):silicon} had its laws changed by an ion storm to {laws.LoggingString()}");

        var ev = new IonStormLawsEvent(laws);
        RaiseLocalEvent(ent, ref ev);
    }
}
