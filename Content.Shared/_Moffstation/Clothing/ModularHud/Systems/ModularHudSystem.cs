using System.Linq;
using Content.Shared._Moffstation.Clothing.ModularHud.Components;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Chemistry;
using Content.Shared.Contraband;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Flash;
using Content.Shared.Foldable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using static Content.Shared._Moffstation.Clothing.ModularHud.Components.ModularHudVisualKeys;

namespace Content.Shared._Moffstation.Clothing.ModularHud.Systems;

// TODO CENT Document
public sealed partial class ModularHudSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly BlurryVisionSystem _blurryVision = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedVerbSystem _verb = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;


    /// This list specifies the layers and (by implicit ordering) precedence for lens visuals. That is to say, the
    /// highest priority lens color will apply to `Lens`, then the next highest will apply to `LensAccentMajor`, and so on.
    private static readonly List<ModularHudVisualKeys>
        // ReSharper disable once UseCollectionExpression // Whatever the underlying thing that enables collection expressions for statics is is not whitelisted in Robust, so fuck me, I guess.
        LensVisualKeys = new() { Lens, LensAccentMajor, LensAccentMinor };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModularHudComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ModularHudComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<ModularHudComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ModularHudComponent, GotUnequippedEvent>(OnGotUneqipped);
        SubscribeLocalEvent<ModularHudComponent, FoldedEvent>(OnFolded);
        SubscribeLocalEvent<ModularHudComponent, EntInsertedIntoContainerMessage>(OnContainerModifiedMessage);
        SubscribeLocalEvent<ModularHudComponent, EntRemovedFromContainerMessage>(OnContainerModifiedMessage);
        SubscribeLocalEvent<ModularHudComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ModularHudComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ModularHudComponent, HudModulesRemovalDoAfterEvent>(OnHudModulesRemovalDoAfter);
        SubscribeLocalEvent<ModularHudComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);

        // Relays for module events.
        SubscribeRelaysForEffectEvents<GetContrabandDetailsEvent>();
        SubscribeRelaysForEffectEvents<SolutionScanEvent>();
        SubscribeRelaysForEffectEvents<GetEyeProtectionEvent>();
        SubscribeRelaysForEffectEvents<SeeIdentityAttemptEvent>();
        SubscribeRelaysForEffectEvents<FlashAttemptEvent>();
        SubscribeRelaysForEffectEvents<GetBlurEvent>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<ShowJobIconsComponent>>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<ShowHealthBarsComponent>>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<ShowHealthIconsComponent>>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<ShowHungerIconsComponent>>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<ShowThirstIconsComponent>>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<ShowMindShieldIconsComponent>>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<ShowSyndicateIconsComponent>>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<ShowCriminalRecordIconsComponent>>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<BlackAndWhiteOverlayComponent>>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<NoirOverlayComponent>>();

        // Relays `TArgs` to all contained modules.
        void SubscribeRelaysForEffectEvents<TArgs>() where TArgs : notnull => Subs.SubscribeWithRelay(
            delegate(Entity<ModularHudComponent> entity, ref TArgs args)
            {
                foreach (var module in GetModules(entity))
                {
                    RaiseLocalEvent(module, ref args);
                }
            }
        );
    }

    /// Yields all the modules contained in the given HUD.
    public IEnumerable<Entity<ModularHudModuleComponent>> GetModules(Entity<ModularHudComponent> entity)
    {
        foreach (var moduleEnt in entity.Comp.ModuleContainer?.ContainedEntities ?? [])
        {
            if (TryComp<ModularHudModuleComponent>(moduleEnt, out var moduleComp))
                yield return (moduleEnt, moduleComp);
        }
    }

    private void OnStartup(Entity<ModularHudComponent> entity, ref ComponentStartup args)
    {
        entity.Comp.ModuleContainer = _container.EnsureContainer<Container>(entity, entity.Comp.ModuleContainerId);
        RefreshEffectsForModules(GetModules(entity));
        SyncVisuals(entity);
    }

    private void OnComponentRemove(Entity<ModularHudComponent> entity, ref ComponentRemove args)
    {
        RefreshEffectsForModules(GetModules(entity));
    }

    private void OnInteractUsing(Entity<ModularHudComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Delegate to interaction verbs so I don't have to redefine these.
        var verb = _verb.GetLocalVerbs(args.Target, args.User, new List<Type> { typeof(InteractionVerb) })
            .Where(it => !it.Disabled)
            .FirstOrDefault();
        if (verb is null)
            return;

        verb.Act?.Invoke();
        args.Handled = true;
    }

    private void OnGetInteractionVerbs(Entity<ModularHudComponent> entity, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanComplexInteract)
            return;

        // Module insertion
        if (args.Using is { } used && HasComp<ModularHudModuleComponent>(args.Using))
        {
            LocId? disabledReason = null;
            if (_whitelist.IsWhitelistFail(entity.Comp.ModuleWhitelist, used) ||
                _whitelist.IsWhitelistPass(entity.Comp.ModuleBlacklist, used))
            {
                disabledReason = entity.Comp.ModuleFailsRequirementsErrorText;
            }

            if (entity.Comp.ModuleContainer.ContainedEntities.Count >= entity.Comp.ModuleSlots)
            {
                disabledReason = entity.Comp.ModuleSlutsFullErrorText;
            }

            // All slots full already.
            args.Verbs.Add(new InteractionVerb
            {
                Text = Loc.GetString(entity.Comp.InsertModuleVerbText),
                Act = () =>
                {
                    if (!_container.Insert(used, entity.Comp.ModuleContainer))
                    {
                        // This should always succeed, so error out if it doesn't.
                        this.AssertOrLogError($"Failed to insert {ToPrettyString(used)} into {ToPrettyString(entity)}");
                    }
                },
                IconEntity = GetNetEntity(used),
                Message = Loc.GetString(
                    disabledReason ?? entity.Comp.InsertModuleVerbMessage,
                    ("module", Name(used)),
                    ("hud", Name(entity))
                ),
                Disabled = disabledReason != null,
            });
        }

        // Module removal
        if (GetModules(entity).Any())
        {
            var tool = args.Using;
            var user = args.User;
            var usedHasQuality = args.Using is { } u && _tool.HasQuality(u, entity.Comp.ModuleExtractionMethod);
            _proto.Resolve(entity.Comp.ModuleExtractionMethod, out var toolQuality);
            args.Verbs.Add(new InteractionVerb
            {
                Text = Loc.GetString(entity.Comp.RemoveModulesVerbText),
                Act = () =>
                {
                    _tool.UseTool(
                        tool!.Value, // Can only invoke the verb if `tool` is not null
                        user,
                        entity.Owner,
                        entity.Comp.ModuleRemovalDelay,
                        [entity.Comp.ModuleExtractionMethod],
                        new HudModulesRemovalDoAfterEvent(),
                        out _
                    );
                },
                Message = Loc.GetString(
                    usedHasQuality
                        ? entity.Comp.RemoveModulesVerbMessage
                        : entity.Comp.MissingToolQualityErrorText,
                    ("quality", Loc.GetString(toolQuality?.Name ?? "Unknown")),
                    ("hud", Name(entity))
                ),
                Disabled = !usedHasQuality,
                Icon = toolQuality?.Icon,
            });
        }
    }

    /// Describes what, if anything, is in this HUD.
    private void OnExamined(Entity<ModularHudComponent> entity, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(ModularHudComponent)))
        {
            using var modules = GetModules(entity).GetEnumerator();
            if (!modules.MoveNext())
            {
                args.PushMarkup(Loc.GetString(entity.Comp.NoModulesExamineText));
                return;
            }

            args.PushMarkup(Loc.GetString(entity.Comp.HeaderExamineText));
            do
            {
                args.PushMarkup(Loc.GetString(entity.Comp.ModuleItemExamineText, ("module", modules.Current)));
            } while (modules.MoveNext());
        }
    }

    /// Removes all modules from this HUD when the doafter is completed.
    private void OnHudModulesRemovalDoAfter(Entity<ModularHudComponent> entity, ref HudModulesRemovalDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        foreach (var module in GetModules(entity).ToList())
        {
            _container.Remove(module.Owner, entity.Comp.ModuleContainer);
            _hands.TryPickup(args.User, module);
        }
    }

    /// Refresh the effects provided by the module added/removed.
    private void OnContainerModifiedMessage<TArgs>(
        Entity<ModularHudComponent> entity,
        ref TArgs args
    ) where TArgs : ContainerModifiedMessage
    {
        if (args.Container.ID != entity.Comp.ModuleContainerId ||
            !TryComp<ModularHudModuleComponent>(args.Entity, out var moduleComp))
            return;

        RefreshEffectsForModules([(args.Entity, moduleComp)]);
        SyncVisuals(entity);
    }

    private void OnGotEquipped(Entity<ModularHudComponent> entity, ref GotEquippedEvent args)
    {
        RefreshEffectsForWearerForContainedModules(entity, args.Equipee);
    }

    private void OnGotUneqipped(Entity<ModularHudComponent> entity, ref GotUnequippedEvent args)
    {
        RefreshEffectsForWearerForContainedModules(entity, args.Equipee);
    }

    private void OnFolded(Entity<ModularHudComponent> entity, ref FoldedEvent args) => SyncVisuals(entity);

    private void RefreshEffectsForWearerForContainedModules(Entity<ModularHudComponent> entity, EntityUid equippee)
    {
        _blurryVision.UpdateBlurMagnitude(equippee);
        var flashEv = new FlashImmunityChangedEvent(_flash.IsFlashImmune(equippee));
        RaiseLocalEvent(equippee, ref flashEv);
        RefreshEffectsForModules(GetModules(entity));
        SyncVisuals(entity);
    }

    private void RefreshEffectsForModules(IEnumerable<Entity<ModularHudModuleComponent>> modules)
    {
        var ev = new EquipmentHudNeedsRefreshEvent();
        foreach (var module in modules)
        {
            RaiseLocalEvent(module, ref ev);
        }
    }

    /// Sets <see cref="AppearanceComponent"/> data for the given HUD entity, as appropriate. This causes the client to
    /// update the visuals for the HUD entity.
    private void SyncVisuals(Entity<ModularHudComponent, ModularHudVisualsComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp2))
            return;

        var visuals = new Dictionary<ModularHudVisuals, PriorityQueue<ModularHudModuleColor>>()
        {
            // Lens gets three colors because there're three lens layers.
            [ModularHudVisuals.Lens] = new(3),
            [ModularHudVisuals.Accent] = new(1),
            [ModularHudVisuals.Specular] = new(1),
        };
        foreach (var (layer, color) in GetModules(entity)
                     .SelectMany(module => module.Comp.Visuals)
                     .Concat(entity.Comp2.InnateVisuals))
        {
            visuals[layer].Add(color);
        }

        var appearance = CompOrNull<AppearanceComponent>(entity);
        // Clear lens accents because we don't always have them.
        _appearance.RemoveData(entity, LensAccentMinor, appearance);
        _appearance.RemoveData(entity, LensAccentMajor, appearance);
        foreach (var (layer, data) in visuals)
        {
            // Special handling for lens layers since there're three.
            if (layer == ModularHudVisuals.Lens && data.Count > 1)
            {
                foreach (var (color, key) in data.Zip(LensVisualKeys))
                {
                    _appearance.SetData(entity, key, color.Color, appearance);
                }
            }
            else
            {
                // Other layers, or when there's only one lens color
                var color = data.TakeOrNull()?.Color ?? entity.Comp2.DefaultVisuals[layer];
                _appearance.SetData(entity, VisualsLayerToKey(layer), color, appearance);
            }
        }

        if (TryComp<FoldableComponent>(entity, out var foldable))
        {
            _appearance.SetData(entity, FoldableModularHudVisuals.Key, foldable.IsFolded, appearance);
        }
        else
        {
            _appearance.RemoveData(entity, FoldableModularHudVisuals.Key, appearance);
        }
    }

    private ModularHudVisualKeys VisualsLayerToKey(ModularHudVisuals layer) => layer switch
    {
        ModularHudVisuals.Accent => Accent,
        ModularHudVisuals.Specular => Specular,
        ModularHudVisuals.Lens => Lens,
        _ => this.Unreachable<ModularHudVisualKeys>($"Unknown {nameof(ModularHudVisualKeys)} value: {layer}"),
    };

    /// This doafter event is raised when the doafter to remove the HUD's modules is complete.
    [Serializable, NetSerializable]
    private sealed partial class HudModulesRemovalDoAfterEvent : SimpleDoAfterEvent;
}
