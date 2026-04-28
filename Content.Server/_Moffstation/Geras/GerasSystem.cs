using System.Linq;
using Content.Shared.Zombies;
using Content.Server.Actions;
using Content.Server.Body;
using Content.Server.Inventory;
using Content.Server.Popups;
using Content.Shared._Moffstation.Body.Events;
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
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Preferences;
using Content.Shared.Projectiles;
using Content.Shared.StatusEffect;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Temperature.Components;
using Content.Shared.Traits.Assorted;
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
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly SharedStaminaSystem  _stamina = default!;

    private const string GerasIdSlot = "id";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GerasComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GerasComponent, MorphGeras>(OnMorphIntoGeras);
        SubscribeLocalEvent<GerasComponent, ComponentShutdown>(OnRemoveGeras);
        SubscribeLocalEvent<GerasComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GerasComponent, EntityZombifiedEvent>(OnZombification);
        SubscribeLocalEvent<GerasComponent, GerasVisualInitEvent>(OnGerasVisualInit);
        SubscribeLocalEvent<EnsnareableComponent, PreMorphGerasEvent>(OnRemoveSnares);
        SubscribeLocalEvent<EmbeddedContainerComponent, PreMorphGerasEvent>(OnRemoveProjectiles);
        SubscribeLocalEvent<DamageableComponent, PostMorphGerasEvent>(OnTransferDamage);
        SubscribeLocalEvent<BloodstreamComponent, PostMorphGerasEvent>(OnTransferBloodstream);
        SubscribeLocalEvent<TemperatureComponent, PostMorphGerasEvent>(OnTransferTemperature);
        SubscribeLocalEvent<FlammableComponent, PreMorphGerasEvent>(OnTransferFire);
        SubscribeLocalEvent<StorageComponent, PreMorphGerasEvent>(OnTransferStorage);
        SubscribeLocalEvent<StatusEffectsComponent, PreMorphGerasEvent>(OnTransferOldStatus);
        SubscribeLocalEvent<StatusEffectContainerComponent, PreMorphGerasEvent>(OnTransferNewStatus);
        SubscribeLocalEvent<HungerComponent, PreMorphGerasEvent>(OnTransferHunger);
        SubscribeLocalEvent<ThirstComponent, PreMorphGerasEvent>(OnTransferThirst);
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

        var preGerasEv = new PreMorphGerasEvent(geras);
        RaiseLocalEvent(uid, ref preGerasEv);


        // TODO: Do this without the ID getting its own special thing
        // We do this because we may unequip the jumpsuit which drops the ID prior to attempt to transfer the ID
        if (_inventory.TryGetSlotEntity(uid, GerasIdSlot, out var id) && id is { } idUid)
        {
            _inventory.TryEquip(geras, idUid, GerasIdSlot, true, true);
        }

        // Reequip or drop all inventory items
        if (_inventory.TryGetContainerSlotEnumerator(uid, out var enumerator))
        {
            while (enumerator.MoveNext(out var slot))
            {
                if (!_inventory.HasSlot(geras, slot.ID) ||
                    slot.ContainedEntity is not { } containedEntity ||
                    !_inventory.TryEquip(geras, containedEntity, slot.ID, true, true))
                {
                    _inventory.TryUnequip(uid, slot.ID, true, true);
                }
            }
        }
        foreach (var held in _hands.EnumerateHeld(uid))
        {
            _hands.TryDrop(uid, held);
        }

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
                _container.AttachParentToContainerOrGrid((uid, Transform(uid)));
            }
        }
        if (_container.IsEntityOrParentInContainer(uid))// If the entity is in any other container, put the geras in that container
        {
            _transform.DropNextTo(geras, uid);
        }

        _implantSystem.TransferImplants(uid, geras);

        var playerTransform = Transform(uid);
        var gerasTransform = Transform(geras);

        if (TerminatingOrDeleted(playerTransform.ParentUid))
            return;

        // Swap positions of initial entity and geras
        _transform.SetParent(geras, gerasTransform, playerTransform.ParentUid);
        _transform.SetCoordinates(geras, gerasTransform, playerTransform.Coordinates, playerTransform.LocalRotation);
        BanishEntity((uid, component, playerTransform));

        var postMorphEv = new PostMorphGerasEvent(uid);
        RaiseLocalEvent(geras, ref postMorphEv);

        //Transfer Stomach Contents
        var getStomachEv = new GetStomachContentsEvent();
        RaiseLocalEvent(uid, ref getStomachEv);
        if (getStomachEv.Handled)
        {
            var setStomachEv = new ApplyStomachContentsEvent(getStomachEv.Contents);
            RaiseLocalEvent(geras, ref setStomachEv);
            var clearStomachEv = new EmptyStomachEvent();
            RaiseLocalEvent(uid, ref clearStomachEv);
        }

        // Clear Stamina effects
        _stamina.TryTakeStamina(geras, _stamina.GetStaminaDamage(uid));
        RaiseLocalEvent(uid, new ClearStaminaDamageEvent());

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

    private void OnRemoveSnares(Entity<EnsnareableComponent> ent, ref PreMorphGerasEvent args)
    {
        foreach (Entity<EnsnaringComponent?> bola in ent.Comp.Container.ContainedEntities.ToList())
        {
            if (TryComp<EnsnaringComponent>(bola, out var ensnaringComponent))
                _ensnareable.ForceFree(bola, ensnaringComponent);
        }
    }

    private void OnRemoveProjectiles(Entity<EmbeddedContainerComponent> ent, ref PreMorphGerasEvent args)
    {
        foreach (var projectile in ent.Comp.EmbeddedObjects)
        {
            if(TryComp<EmbeddableProjectileComponent>(projectile, out var embedComp))
                _projectile.EmbedDetach(projectile, embedComp);
        }
    }

    private void OnTransferDamage(Entity<DamageableComponent> ent, ref PostMorphGerasEvent args)
    {
        if (_mobThreshold.GetScaledDamage(args.Parent, ent.Owner, out var damage) &&
            damage != null)
        {
            _damageable.SetDamage((ent.Owner, ent.Comp), damage);
            _damageable.ClearAllDamage(args.Parent);
        }
    }

    private void OnTransferBloodstream(Entity<BloodstreamComponent> ent, ref PostMorphGerasEvent args)
    {
        if (TryComp<BloodstreamComponent>(args.Parent, out var bloodstreamParent))
        {
            //Empty Geras Bloodstream
            _bloodstream.ClearBloodStream(ent);

            if (_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution)
                && _solutionContainer.ResolveSolution(args.Parent, bloodstreamParent.BloodSolutionName, ref bloodstreamParent.BloodSolution))
            {
                //Transfer blood level
                _bloodstream.TryModifyBloodLevel(ent.Owner, _bloodstream.GetBloodLevel(args.Parent)*bloodstreamParent.BloodReferenceSolution.Volume);

                //Transfer other chemicals (needs to be separate b/c of blood not necessarily being blood)
                var ev = new MetabolismExclusionEvent();
                RaiseLocalEvent(args.Parent, ref ev);

                foreach (var (reagent, quantity) in bloodstreamParent.BloodSolution.Value.Comp.Solution.Contents.ToList())
                {
                    if (ev.Reagents.Contains(reagent))
                        continue;

                    _solutionContainer.TryAddReagent(ent.Comp.BloodSolution.Value, reagent.Prototype, quantity);
                }
            }

            if (_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.MetabolitesSolutionName, ref ent.Comp.MetabolitesSolution)
                && _solutionContainer.ResolveSolution(args.Parent, bloodstreamParent.MetabolitesSolutionName, ref bloodstreamParent.MetabolitesSolution))
            {
                foreach (var (reagent, quantity) in bloodstreamParent.MetabolitesSolution.Value.Comp.Solution.Contents.ToList())
                {
                    _solutionContainer.TryAddReagent(ent.Comp.MetabolitesSolution.Value, reagent.Prototype, quantity);
                }
            }
        }
    }

    private void OnTransferTemperature(Entity<TemperatureComponent> ent, ref PostMorphGerasEvent args)
    {
        if (TryComp<TemperatureComponent>(args.Parent, out var parentTemp))
        {
            ent.Comp.CurrentTemperature = parentTemp.CurrentTemperature;
        }
    }

    private void OnTransferFire(Entity<FlammableComponent> ent, ref PreMorphGerasEvent args)
    {
        //But won't unmix the fuel from them
        if (TryComp<FlammableComponent>(args.Geras, out var flammableGeras))
        {
            flammableGeras.FireStacks = ent.Comp.FireStacks;
        }
    }

    private void OnTransferStorage(Entity<StorageComponent> ent, ref PreMorphGerasEvent args)
    {
        foreach (var item in ent.Comp.StoredItems.Keys.ToList())
        {
            _storage.InsertAt(args.Geras, item, ent.Comp.StoredItems[item], out _, ent.Owner, playSound: false);
        }
    }

    private void OnTransferOldStatus(Entity<StatusEffectsComponent> ent, ref PreMorphGerasEvent args)
    {
        var oldStatusTransferEv = new TransferStatusesEvent(ent);
        RaiseLocalEvent(args.Geras, ref oldStatusTransferEv);
    }

    private void OnTransferNewStatus(Entity<StatusEffectContainerComponent> ent, ref PreMorphGerasEvent args)
    {
        EnsureComp<StatusEffectContainerComponent>(args.Geras);
        var newStatusTransferEv = new TransferNewStatusEffectsEvent(ent);
        RaiseLocalEvent(args.Geras, ref newStatusTransferEv);
    }

    private void OnTransferHunger(Entity<HungerComponent> ent, ref PreMorphGerasEvent args)
    {
        if (TryComp<HungerComponent>(args.Geras, out var gerasHunger))
        {
            _hunger.SetHunger(args.Geras, _hunger.GetHunger(ent.Comp), gerasHunger);
        }
    }

    private void OnTransferThirst(Entity<ThirstComponent> ent, ref PreMorphGerasEvent args)
    {
        if (TryComp<ThirstComponent>(args.Geras, out var gerasThirst))
        {
            _thirst.SetThirst(args.Geras, gerasThirst, ent.Comp.CurrentThirst);
        }
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
        if (args.Profile != null)
        {
            _bodySystem.ApplyProfile(geras, new() { SkinColor = args.Profile.Appearance.SkinColor });
            _profileSystem.ApplyProfileTo((geras, EnsureComp<HumanoidProfileComponent>(geras)), args.Profile);
        }

        if (TryComp<UnrevivableComponent>(uid, out var parentUnrev))
        {
            var gerasUnrev = EnsureComp<UnrevivableComponent>(geras);
            gerasUnrev.Cloneable = parentUnrev.Cloneable;
        }

        uid.Comp.VisualsLoaded = true;
    }

    private void OnRemoveGeras(EntityUid uid, GerasComponent component, ComponentShutdown args)
    {
        QueueDel(component.Geras);
        QueueDel(component.StorageMap);
    }
}

public record struct GerasVisualInitEvent(HumanoidCharacterProfile? Profile);

[ByRefEvent]
public record struct PreMorphGerasEvent(EntityUid Geras);

[ByRefEvent]
public record struct PostMorphGerasEvent(EntityUid Parent);
