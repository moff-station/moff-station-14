using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Shared.Paper;
using Content.Shared.Whitelist;
using Content.Shared.Fax.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Starlight.Paper;

public sealed class GameruleOnSignSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameruleOnSignComponent, PaperSignedEvent>(OnPaperSigned);
        SubscribeLocalEvent<GameruleOnSignComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, GameruleOnSignComponent comp, ComponentInit init)
    {
        // Remove faxable, so triggers cant be duped. can be readded by admins, and the destination copy will remove it automatically
        if (HasComp<FaxableObjectComponent>(uid))
            RemComp<FaxableObjectComponent>(uid);
    }


    private void OnPaperSigned(EntityUid uid, GameruleOnSignComponent component, PaperSignedEvent args)
    {
        // Check if they've already signed the paper, if not add them to the list
        if (!component.SignedEntityUids.Add(args.Signer))
            return;

        if (!_whitelistSystem.CheckBoth(args.Signer, component.Blacklist, component.Whitelist))
            return;

        if (component.SignaturesNeeded > 0)
        {
            component.SignaturesNeeded--;

            if (component.SignaturesNeeded <= 0 && _random.NextFloat() < component.GameruleChance)
            {
                foreach (var rule in component.Rules)
                {
                    var ent = _gameTicker.AddGameRule(rule.Id);
                    _gameTicker.StartGameRule(ent);
                }
            }
        }

        if (component.AntagCharges > 0)
        {
            component.AntagCharges--;

            foreach (var antag in component.Antags)
            {
                // var targetComp = _componentFactory.GetComponent(antag.TargetComponent);
                if (!TryComp(args.Signer, out ActorComponent? actor))
                    return;
                // Evil function (though usage is probably correct). There isn't infrastructure around gamerules targeting specific people, so we'll just hit em with this.
                _antag.ForceMakeAntag<AntagSelectionComponent>(actor.PlayerSession, antag);
            }
        }
    }
}
