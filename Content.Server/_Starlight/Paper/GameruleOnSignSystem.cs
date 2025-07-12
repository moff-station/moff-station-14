using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared._Starlight.Paper;
using Content.Shared.Paper;
using Content.Shared.Whitelist;
using Content.Shared.Fax.Components;
using Content.Shared.Mind;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Starlight.Paper;

public sealed class GameruleOnSignSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly MindSystem _mind = default!;



    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameruleOnSignComponent, PaperSignedEvent>(OnPaperSigned);
    }

    private void OnPaperSigned(Entity<GameruleOnSignComponent> ent, ref PaperSignedEvent args)
    {
        // Check if they've already signed the paper, if not add them to the list
        if (!ent.Comp.SignedEntityUids.Add(args.Signer))
            return;

        if (!_whitelistSystem.CheckBoth(args.Signer, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        if (ent.Comp.SignaturesNeeded > 0)
        {
            ent.Comp.SignaturesNeeded--;

            if (ent.Comp.SignaturesNeeded <= 0 && _random.NextFloat() < ent.Comp.GameruleChance)
            {
                foreach (var rule in ent.Comp.Rules)
                {
                    var ruleEnt = _gameTicker.AddGameRule(rule.Id);
                    _gameTicker.StartGameRule(ruleEnt);
                }
            }
        }

        if (ent.Comp.Charges > 0 && _random.NextFloat() < ent.Comp.TriggerChance)
        {
            // If it can't do it's job right, then dont subtract from the charges
            if (!TryComp(args.Signer, out ActorComponent? actor))
                return;

            if (!_mind.TryGetMind(actor.PlayerSession.UserId, out var mind, out var mindComponent))
                return;

            ent.Comp.Charges--;


            foreach (var antag in ent.Comp.Antags)
            {
                // var targetComp = _ent.CompFactory.GetComponent(antag.TargetComponent);
                // Evil function (though usage is probably correct). There isn't infrastructure around gamerules targeting specific people, so we'll just hit em with this.
                _antag.ForceMakeAntag<AntagSelectionComponent>(actor.PlayerSession, antag);
            }

            if (ent.Comp.Objectives.Count > 0)
            {
                // Wipe all the current objectives so they can be overriden
                while (_mind.TryRemoveObjective(mind.Value, mindComponent, 0))
                {
                    // Wipe all the current objectives so they can be overriden
                }

                foreach (var objective in ent.Comp.Objectives)
                {
                    _mind.TryAddObjective(mind.Value, mindComponent, objective.Id);
                }
            }
        }
    }
}
