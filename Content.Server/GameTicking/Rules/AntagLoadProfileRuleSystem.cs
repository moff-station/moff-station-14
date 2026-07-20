using Content.Server._Moffstation.Voting; // Moff - enrollment character selection
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Preferences.Managers;
using Content.Shared.Body;
using Content.Shared.DetailExaminable;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed partial class AntagLoadProfileRuleSystem : GameRuleSystem<AntagLoadProfileRuleComponent>
{
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private IServerPreferencesManager _prefs = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private MetaDataSystem _metaData = default!;  // Moffstation
    [Dependency] private MoffEnrollEventSystem _moffEnroll = default!; // Moff - enrollment character selection

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagLoadProfileRuleComponent, AntagSelectEntityEvent>(OnSelectEntity);
    }

    private void OnSelectEntity(Entity<AntagLoadProfileRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Handled)
            return;

        // Moff start - enrollees can opt to spawn as a randomly generated character rather than their selected one.
        var profile = args.Session == null || _moffEnroll.EnrolleeWantsRandom(ent.Owner, args.Session)
            ? HumanoidCharacterProfile.RandomWithSpecies()
            : _prefs.GetPreferences(args.Session.UserId).SelectedCharacter as HumanoidCharacterProfile;
        // Moff end

        if (profile?.Species is not { } speciesId || !ProtoMan.Resolve(speciesId, out var species))
        {
            species = ProtoMan.Index(HumanoidCharacterProfile.DefaultSpecies);
        }

        if (ent.Comp.SpeciesOverride != null
            && (ent.Comp.SpeciesOverrideBlacklist?.Contains(new ProtoId<SpeciesPrototype>(species.ID)) ?? false))
        {
            species = ProtoMan.Index(ent.Comp.SpeciesOverride.Value);
        }

        args.Entity = Spawn(species.Prototype, args.Coords);
        if (profile?.WithSpecies(species.ID) is { } humanoidProfile)
        {
            _visualBody.ApplyProfileTo(args.Entity.Value, humanoidProfile);
            _humanoidProfile.ApplyProfileTo(args.Entity.Value, humanoidProfile);
            // Moffstation - Start - Preserve character info option
            if (ent.Comp.PreserveName)
            {
                _metaData.SetEntityName(args.Entity.Value, humanoidProfile.Name);
                var details = EnsureComp<DetailExaminableComponent>(args.Entity.Value);
                details.Content = humanoidProfile.FlavorText;
            }
            // Moffstation - End
        }
    }
}
