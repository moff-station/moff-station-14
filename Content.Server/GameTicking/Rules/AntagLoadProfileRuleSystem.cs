using System.Linq;
using Content.Server._Moffstation.Station;
using Content.Server._Moffstation.Voting;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Preferences.Managers;
using Content.Shared.Antag;
using Content.Shared.Body;
using Content.Shared.DetailExaminable;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed partial class AntagLoadProfileRuleSystem : GameRuleSystem<AntagLoadProfileRuleComponent>
{
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private IServerPreferencesManager _prefs = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;

    // Moff start
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private MoffEnrollEventSystem _moffEnroll = default!;
    [Dependency] private MoffJobCandidateSystem _moffJobCandidate = default!;
    [Dependency] private IRobustRandom _random = default!;
    // Moff end

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagLoadProfileRuleComponent, AntagSelectEntityEvent>(OnSelectEntity);
    }

    private void OnSelectEntity(Entity<AntagLoadProfileRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Handled)
            return;

        // Moff start - Multi character selection for antags + ES ghostrole enrollment votes with selectable profiles
        if (args.Session is { } session &&
            !_moffEnroll.EnrolleeWantsRandom(ent.Owner, args.Session))
        {
            if (args.RespectPlayerSelection)
            {
                // Use the player's active profile (selected by ghostrole enrollment UI)
                args.AssociatedProfile = _prefs.GetPreferences(session.UserId).SelectedCharacter;
            }
            else
            {
                // Pick a random active profile with the antag enabled.
                var antagPrefRoles = args.Antag.PrefRoles;
                var activeProfilesWithAntagEnabled = _moffJobCandidate.GetActiveProfiles(session.UserId)
                    .Where(it => it.AntagPreferences.Intersect(antagPrefRoles).Any())
                    .ToList();
                if (activeProfilesWithAntagEnabled.Count != 0)
                {
                    args.AssociatedProfile = _random.Pick(activeProfilesWithAntagEnabled);
                }
            }
        }

        // If we didn't assign a profile above for whatever reason, make a random one. Alias it to `profile` to
        // minimize changes to upstream code.
        args.AssociatedProfile ??= HumanoidCharacterProfile.Random();
        var profile = args.AssociatedProfile;
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
