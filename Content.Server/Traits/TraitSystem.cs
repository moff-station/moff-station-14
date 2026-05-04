using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.Traits;

public sealed class TraitSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    // Moffstation - Begin - Allow application of traits via other systems.
    /// Attempts to resolve <paramref name="trait"/> and apply it to <paramref name="ent"/>. Returns false if the trait
    /// proto fails to resolve or if <paramref name="ent"/> fails black/whitelist for the trait.
    public bool TryApplyTrait(EntityUid ent, ProtoId<TraitPrototype> trait, bool includeGear)
    {
        if (!_prototypeManager.TryIndex(trait, out var traitPrototype))
        {
            Log.Error($"No trait found with ID {trait}!");
            return false;
        }

        if (_whitelistSystem.IsWhitelistFail(traitPrototype.Whitelist, ent) ||
            _whitelistSystem.IsWhitelistPass(traitPrototype.Blacklist, ent))
            return false;

        // Add all components required by the prototype
        if (traitPrototype.Components.Count > 0)
            EntityManager.AddComponents(ent, traitPrototype.Components, false);

        // Add all JobSpecials required by the prototype
        foreach (var special in traitPrototype.Specials)
        {
            special.AfterEquip(ent);
        }

        // Add item required by the trait
        if (!includeGear || traitPrototype.TraitGear == null)
            return true;

        if (!TryComp(ent, out HandsComponent? handsComponent))
            return true;

        var coords = Transform(ent).Coordinates;
        var inhandEntity = Spawn(traitPrototype.TraitGear, coords);
        _sharedHandsSystem.TryPickup(ent,
            inhandEntity,
            checkActionBlocker: false,
            handsComp: handsComponent);

        return true;
    }
    // Moffstation - End

    // When the player is spawned in, add all trait components selected during character creation
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        // Check if player's job allows to apply traits
        if (args.JobId == null ||
            !_prototypeManager.Resolve<JobPrototype>(args.JobId, out var protoJob) ||
            !protoJob.ApplyTraits)
        {
            return;
        }

        foreach (var traitId in args.Profile.TraitPreferences)
        {
            TryApplyTrait(args.Mob, traitId, includeGear: true); // Moffstation - Make trait application reuseable. The implementation of `TryApplyTrait` above was extracted from here, so if changes are made to deleted code here, apply them to that function.
        }

        //Moffstation - geras traits - begin
        var ev = new TraitsAppliedEvent(args.Profile);
        RaiseLocalEvent(args.Mob, ref ev);
        //Moffstation - end
    }
}

// Moffstation - Begin - Allow application of traits by other systems
[ByRefEvent]
public readonly record struct TraitsAppliedEvent(HumanoidCharacterProfile Profile);
// Moffstation - Ent
