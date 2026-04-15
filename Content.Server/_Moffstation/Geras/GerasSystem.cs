using System.Linq;
using Content.Shared.Zombies;
using Content.Server.Actions;
using Content.Server.Body;
using Content.Server.Inventory;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared._Moffstation.Damage.Events;
using Content.Shared._Moffstation.Geras;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Implants;
using Content.Shared.Kitchen.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Preferences;
using Content.Shared.Projectiles;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffect;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Temperature.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._Moffstation.Geras;

/// <summary>
/// Geras is the god of old age, and A geras is the small morph of a slime. This system allows the slimes to have the morphing action.
/// </summary>
public sealed class GerasSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly VisualBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly ServerInventorySystem _inventory = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implantSystem = default!;
    [Dependency] private readonly HumanoidProfileSystem _profileSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedEnsnareableSystem _ensnareable = default!;
    [Dependency] private readonly SharedProjectileSystem _projectile = default!;


    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GerasComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GerasComponent, MorphGeras>(OnMorphIntoGeras);
        SubscribeLocalEvent<GerasComponent, ComponentShutdown>(OnRemoveGeras);
        SubscribeLocalEvent<GerasComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GerasComponent, EntityZombifiedEvent>(OnZombification);
        SubscribeLocalEvent<GerasComponent, GerasVisualInitEvent>(OnGerasVisualInit);
    }

    private void OnInit(Entity<GerasComponent> ent, ref ComponentInit args)
    {
        ent.Comp.StorageMap = _map.CreateMap(runMapInit: false);
    }

    private void OnZombification(EntityUid uid, GerasComponent component, EntityZombifiedEvent args)
    {
        _actionsSystem.RemoveAction(uid, component.GerasActionEntity);
    }

    private void OnMapInit(EntityUid uid, GerasComponent component, MapInitEvent args)
    {
        // create the geras entity and store it
        if (component.GerasProto != null)
        {
            var geras = Spawn(component.GerasProto, _transform.GetMapCoordinates(uid, Transform(uid)), rotation: _transform.GetWorldRotation(uid));
            component.Geras = geras;

            //tie the created entity's geras component to the original for reverse transformation
            var metaGerasComponent = EnsureComp<GerasComponent>(geras);
            metaGerasComponent.Geras = uid;

            BanishEntity((geras, metaGerasComponent, Transform(geras)));

            _metaData.SetEntityName(geras, Name(uid));
            if (TryComp<HumanoidProfileComponent>(uid, out var profile))
                _bodySystem.ApplyProfile(geras, new() { SkinColor = profile.SkinColor });

            //no need to load name/color for the initial player
            metaGerasComponent.VisualsLoaded = true;
        }

        // try to add geras action
        _actionsSystem.AddAction(uid, ref component.GerasActionEntity, component.GerasAction);
    }

    private void OnMorphIntoGeras(EntityUid uid, GerasComponent component, MorphGeras args)
    {
        if (HasComp<ZombieComponent>(uid))
            return; // i hate zomber.

        if (!component.Geras.HasValue)
            return;

        var geras = component.Geras.Value;


        // Drop all inventory items
        if (_inventory.TryGetContainerSlotEnumerator(uid, out var enumerator))
        {
            while (enumerator.MoveNext(out var slot))
            {
                _inventory.TryUnequip(uid, slot.ID, true, true);
            }
        }
        foreach (var held in _hands.EnumerateHeld(uid))
        {
            _hands.TryDrop(uid, held);
        }

        //Remove bolas
        if (TryComp<EnsnareableComponent>(uid, out var ensnared) && ensnared.IsEnsnared)
        {
            foreach (Entity<EnsnaringComponent?> bola in ensnared.Container.ContainedEntities.ToList())
            {
                if (TryComp<EnsnaringComponent>(bola, out var ensnaringComponent))
                    _ensnareable.ForceFree(bola, ensnaringComponent);
            }
        }

        //Remove embedded projectiles
        if (TryComp<EmbeddedContainerComponent>(uid, out var embeddedContainer))
        {
            foreach (var projectile in embeddedContainer.EmbeddedObjects)
            {
                if(TryComp<EmbeddableProjectileComponent>(projectile, out var embedComp))
                    _projectile.EmbedDetach(projectile, embedComp);
            }
        }

        var playerTransform = Transform(uid);
        var gerasTransform = Transform(geras);

        // Prevent transform jank
        if (_container.IsEntityInContainer(uid) && _container.TryGetContainingContainer(uid, out var container))
        {
            // If the entity is being held, make the holder drop it
            if (HasComp<HandsComponent>(container.Owner))
            {
                _hands.TryDrop(container.Owner, uid);
            }
            else if (HasComp<StorageComponent>(container.Owner) || HasComp<KitchenSpikeComponent>(container.Owner))// If the entity is in a bag or meatspike, take them out of it
            {
                _container.AttachParentToContainerOrGrid((uid, playerTransform));
            }
        }
        if (_container.IsEntityOrParentInContainer(uid))// If the entity is in any other container, put the geras in that container
        {
            _transform.DropNextTo(geras, uid);
        }

        if (TerminatingOrDeleted(playerTransform.ParentUid))
            return;

        // Swap positions of initial entity and geras
        _transform.SetParent(geras, gerasTransform, playerTransform.ParentUid);
        _transform.SetCoordinates(geras, gerasTransform, playerTransform.Coordinates, playerTransform.LocalRotation);
        BanishEntity((uid, component, playerTransform));

        // Apply damage to the new form
        if (TryComp<DamageableComponent>(geras, out var damageParent) &&
            _mobThreshold.GetScaledDamage(uid, geras, out var damage) &&
            damage != null)
        {
            _damageable.SetDamage((geras, damageParent), damage);
            _damageable.ClearAllDamage(uid);
        }

        // Clear Stamina effects
        RaiseLocalEvent(uid, new ClearStaminaDamageEvent());

        // Transfer bloodstream
        if (TryComp<BloodstreamComponent>(geras, out var bloodstreamGeras)
            && TryComp<BloodstreamComponent>(uid, out var bloodstreamParent))
        {
            if (_solutionContainer.ResolveSolution(geras, bloodstreamGeras.BloodSolutionName, ref bloodstreamGeras.BloodSolution)
                && _solutionContainer.ResolveSolution(uid, bloodstreamParent.BloodSolutionName, ref bloodstreamParent.BloodSolution))
            {
                //stop bleeding
                _bloodstream.TryModifyBleedAmount(uid, -bloodstreamParent.BleedAmount);

                //Empty Geras Bloodstream
                _solutionContainer.RemoveAllSolution((geras, bloodstreamGeras.BloodSolution));

                //Transfer blood level
                _bloodstream.TryModifyBloodLevel(geras, _bloodstream.GetBloodLevel(uid)*bloodstreamParent.BloodReferenceSolution.Volume);

                //Transfer other chemicals (needs to be separate b/c of blood not necessarily being blood)
                var ev = new MetabolismExclusionEvent();
                RaiseLocalEvent(uid, ref ev);

                foreach (var (reagent, quantity) in bloodstreamParent.BloodSolution.Value.Comp.Solution.Contents.ToList())
                {
                    if (ev.Reagents.Contains(reagent))
                        continue;

                    _solutionContainer.TryAddReagent(bloodstreamGeras.BloodSolution.Value, reagent.Prototype, quantity);
                }
            }
        }

        //Transfer Temperature
        if (TryComp<TemperatureComponent>(uid, out var parentTemp) && TryComp<TemperatureComponent>(geras, out var gerasTemp))
        {
            gerasTemp.CurrentTemperature = parentTemp.CurrentTemperature;
        }

        //Transfer fire
        if (TryComp<FlammableComponent>(uid, out var flammableParent))
        {
            //Gerasing quenches the player
            flammableParent.OnFire = false;

            //But won't unmix the fuel from them
            if (TryComp<FlammableComponent>(geras, out var flammableGeras))
            {
                flammableGeras.FireStacks = flammableParent.FireStacks;
            }
        }

        // Transfer storage to transformed entity
        if (HasComp<StorageComponent>(geras) && TryComp<StorageComponent>(uid, out var parentStorage))
        {
            foreach (var item in parentStorage.StoredItems.Keys.ToList())
            {
                _storage.InsertAt(geras, item, parentStorage.StoredItems[item], out _, uid, playSound: false);
            }
        }

        _implantSystem.TransferImplants(uid, geras);

        if (TryComp<StatusEffectsComponent>(uid, out var oldStatuses))
        {
            var oldStatusTransferEv = new TransferStatusesEvent((uid, oldStatuses));
            RaiseLocalEvent(geras, oldStatusTransferEv);
        }

        // Transfer mind
        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, geras, mind: mind);


        _popupSystem.PopupPredicted(
            Loc.GetString("geras-popup-morph-message-user"),
            Loc.GetString("geras-popup-morph-message-others", ("entity", geras)),
            geras,
            geras
        );

        args.Handled = true;
    }

    /// <summary>
    /// Sends an entity to the void for storage
    /// </summary>
    /// <param name="ent">the entity to be banished</param>
    private void BanishEntity(Entity<GerasComponent, TransformComponent> ent)
    {
        _transform.SetParent(ent, ent.Comp2, ent.Comp1.StorageMap);
    }

    private void OnGerasVisualInit(Entity<GerasComponent> uid, ref GerasVisualInitEvent args)
    {
        if (uid.Comp.Geras is not {} geras)
            return;

        _metaData.SetEntityName(geras, Name(uid));
        if (args.profile != null)
        {
            _bodySystem.ApplyProfile(geras, new() { SkinColor = args.profile.Appearance.SkinColor });
            _profileSystem.ApplyProfileTo((geras, EnsureComp<HumanoidProfileComponent>(geras)), args.profile);
        }

        uid.Comp.VisualsLoaded = true;
    }

    private void OnRemoveGeras(EntityUid uid, GerasComponent component, ComponentShutdown args)
    {
        QueueDel(component.Geras);
        QueueDel(component.StorageMap);
    }
}

public record struct GerasVisualInitEvent(HumanoidCharacterProfile? profile);
