using Content.Shared._Moffstation.Clothing.ModularHud.Systems;
using Content.Shared.Foldable;
using Content.Shared.Inventory;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Clothing.ModularHud.Components;

/// This component marks the given entity as a Modular HUD. This means it can have entities with
/// <see cref="ModularHudModuleComponent"/>s inserted, and when worn in <see cref="ActiveSlots"/>, conveys the effects
/// of those modules to the wearer.
/// <seealso cref="SharedModularHudSystem"/>
/// <seealso cref="ModularHudModuleComponent"/>
/// <seealso cref="ModularHudVisualsComponent"/>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedModularHudSystem))]
public sealed partial class ModularHudComponent : Component
{
    /// While worn in these slots, the HUD conveys its effects to the wearer.
    [DataField]
    public SlotFlags ActiveSlots = SlotFlags.WITHOUT_POCKET;

    /// The ID of <see cref="ModuleContainer"/>.
    [DataField(required: true)]
    public string ModuleContainerId = default!;

    /// The container which contains the entities with <see cref="ModularHudModuleComponent"/> which this HUD contains.
    [ViewVariables]
    public Container? ModuleContainer;

    /// The number of modules in <see cref="ModuleContainer"/>.
    [ViewVariables]
    public int NumContainedModules => ModuleContainer?.Count ?? 0;

    /// The maximum number of modules this HUD can contain.
    [DataField(required: true)]
    public int MaximumContainedModules;

    /// And entity with <see cref="ToolComponent"/> and whose <see cref="ToolComponent.Qualities"/> includes this can
    /// be used to extract the modules from this HUD.
    [DataField(required: true)]
    public ProtoId<ToolQualityPrototype> ModuleExtractionToolQuality;

    /// How long it takes to extract the modules from this HUD.
    [DataField(required: true)]
    public TimeSpan ModuleRemovalDelay;

    // Localization strings and icons for verbs, examination, etc.
    [DataField] public LocId InsertModuleVerbText = "modularhud-verb-insert-module";
    [DataField] public LocId InsertModuleVerbMessage = "modularhud-verb-insert-module-message";

    [DataField]
    public LocId ModuleFailsRequirementsErrorText = "modularhud-verb-insert-module-error-fails-requirements";

    [DataField] public LocId ModuleSlotsFullErrorText = "modularhud-verb-insert-module-error-slots-full";

    [DataField] public LocId RemoveModulesVerbText = "modularhud-verb-remove-modules";
    [DataField] public LocId RemoveModulesVerbMessage = "modularhud-verb-remove-modules-message";
    [DataField] public LocId MissingToolQualityErrorText = "modularhud-verb-remove-modules-error-missing-tool-quality";
    [DataField] public LocId NoModulesToRemovePopupText = "modularhud-verb-remove-modules-error-no-modules-to-remove";

    [DataField] public LocId NoModulesExamineText = "modularhud-examine-no-modules";
    [DataField] public LocId HeaderExamineText = "modularhud-examine-modules-header";
    [DataField] public LocId ModuleItemExamineText = "modularhud-examine-module-item";

    [DataField] public SpriteSpecifier RemoveModuleIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/eject.svg.192dpi.png"));
}

/// Where <see cref="ModularHudComponent"/> conveys functionality, this component confers non-functional dynamic
/// visuals. Basically, allows entities with <see cref="ModularHudComponent"/> to have their sprites modified by the
/// <see cref="ModularHudModuleComponent"/> they contain.
/// <seealso cref="SharedModularHudSystem.SyncVisuals"/>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedModularHudSystem))]
public sealed partial class ModularHudVisualsComponent : Component
{
    /// The colors this HUD use for layers when no modules modify that layer.
    [DataField(required: true)]
    public Dictionary<ModularHudVisuals, Color> DefaultVisuals = default!;

    /// A map of <see cref="ModularHudVisualKeys"/> to layer names, used to determine which layers on the sprite
    /// correspond to which conceptual layers.
    [DataField]
    public Dictionary<ModularHudVisualKeys, string> LayerMap = [];

    /// If an entity with an <see cref="InventoryComponent.SpeciesId"/> in this list wears this HUD, all of its states
    /// will include that species ID. This enables the sort of thing like <c>equipped-EYES</c> versus
    /// <c>equipped-EYES-vox</c>.
    [DataField]
    public List<string> SpeciesWithDifferentClothing = [];

    /// A <see cref="ModularHudVisualsExcludedLayers"/> which describes which layers for which species should not
    /// attempt to be rendered when held.
    [DataField]
    public ModularHudVisualsExcludedLayers InhandExcludedLayers;

    /// A <see cref="ModularHudVisualsExcludedLayers"/> which describes which layers for which species should not
    /// attempt to be rendered when worn.
    // If modular huds ever need to be worn anywhere other than eyes, we'll need to add one of these for each inventory slot.
    [DataField]
    public ModularHudVisualsExcludedLayers EquippedExcludedLayers;

    /// Suffix applied to sprite layer states if this entity is <see cref="FoldableComponent.IsFolded"/>.
    [DataField]
    public string? FoldedLayerSuffix;

    /// <see cref="ModularHudModuleComponent.ModuleColor"/>s included in this entity's visuals regardless of the modules
    /// in it. This is  used, eg. to give sunglasses an innate tint.
    [DataField]
    public Dictionary<ModularHudVisuals, ModularHudModuleComponent.ModuleColor> InnateVisuals = new();

    /// If true, the ModularHudVisualizerSystem will handle rendering the frame layer. This is useful for when, eg.,
    /// the strap "frame" layer of eye patches need to be dynamically flipped.
    [DataField]
    public bool FrameIsDynamic = false;
}

/// Data used to describe which layers the visualizer ought not attempt to include in a sprite based on the wearer's
/// species. Some species' sprites are just too small to include all layers, so they are omitted sometimes.
/// <param name="Default">The set of layers excluded from sprites when no species can be determined, or when <see cref="Species"/> does not include information for the relevant species.</param>
/// <param name="Species">Sets of layers excluded from sprites, keyed by species ID.</param>
[DataRecord, Serializable, NetSerializable]
public readonly partial record struct ModularHudVisualsExcludedLayers(
    HashSet<ModularHudVisualKeys>? Default = null,
    Dictionary<string, HashSet<ModularHudVisualKeys>>? Species = null
);

/// Colorable parts of modular HUDs.
[Serializable, NetSerializable]
public enum ModularHudVisuals : byte
{
    Accent,
    Lens,
    Specular,
}

/// This key is just used to pass whether or not a modular HUD is folded from the main system to the visualizer so that
/// the visualizer doesn't need to interact with the foldable systems.
[Serializable, NetSerializable]
public enum FoldableModularHudVisuals : byte { Key }

/// This component marks an entity as a modular HUD module. It's basically a marker component, but it does also have
/// visual details. In order for a module's effects to work, they need special relaying in <see cref="SharedModularHudSystem"/>.
[RegisterComponent, NetworkedComponent, Access(typeof(SharedModularHudSystem))]
public sealed partial class ModularHudModuleComponent : Component
{
    /// The visuals this module confers to its contained HUD when inserted.
    [DataField]
    public Dictionary<ModularHudVisuals, ModuleColor> Visuals = new();

    [DataField]
    public List<Requirement> Requirements = [];

    /// The requirements for inserting a module into a HUD.
    /// <param name="FailureMessage">What to show the user as an explanation for why the module doesn't fit</param>
    /// <param name="Whitelist">If not null, only HUDs which pass this whitelist may have this module inserted. Does nothing if null.</param>
    /// <param name="Blacklist">If not nulol, HUDs which pass this blacklist may not have this module inserted. Does nothing if null.</param>
    [DataRecord, Serializable, NetSerializable]
    public readonly partial record struct Requirement(
        LocId FailureMessage,
        EntityWhitelist? Whitelist = null,
        EntityWhitelist? Blacklist = null
    );

    /// A color in a <see cref="ModularHudModuleComponent"/>'s visuals.
    /// <param name="Color">The actual color</param>
    /// <param name="Priority">The color's priority. This allows for multiple modules to specify colors for the same layer, though only the highest priority color will be shown.</param>
    [DataRecord, Serializable, NetSerializable]
    public readonly partial record struct ModuleColor(Color Color, int Priority = 0)
        : IComparable<ModuleColor>
    {
        /// Implementation of <see cref="IComparable"/>, just defers to priorities. This is needed to use a priority queue
        /// in <see cref="SharedModularHudSystem.SyncVisuals"/>.
        public int CompareTo(ModuleColor other)
        {
            return Priority.CompareTo(other.Priority);
        }

        /// <summary>The actual color</summary>
        public Color Color { get; init; } = Color;
    }
}

/// This event, when raised on an entity with a HUD component, will refresh the HUD's information. This just triggers
/// <see cref="Content.Client.Overlays.EquipmentHudSystem.RefreshOverlay"/>.
[ByRefEvent]
public record struct EquipmentHudNeedsRefreshEvent;

/// Enum keys for modular HUD's <see cref="AppearanceComponent"/> visuals.
[Serializable, NetSerializable]
public enum ModularHudVisualKeys : byte
{
    Frame,
    Accent,
    Lens,
    Specular,
    LensAccentMinor,
    LensAccentMajor,
}
