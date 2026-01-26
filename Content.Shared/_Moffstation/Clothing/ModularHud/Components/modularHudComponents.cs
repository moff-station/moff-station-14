using Content.Shared._Moffstation.Clothing.ModularHud.Systems;
using Content.Shared.Inventory;
using Content.Shared.Tools;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Clothing.ModularHud.Components;

// TODO CENT Document
[RegisterComponent, NetworkedComponent, Access(typeof(ModularHudSystem))]
public sealed partial class ModularHudComponent : Component
{
    [DataField]
    public SlotFlags ActiveSlots = SlotFlags.WITHOUT_POCKET;

    [DataField(required: true)]
    public string ModuleContainerId = default!;

    [ViewVariables]
    public Container ModuleContainer = default!;

    [DataField(required: true)]
    public int ModuleSlots;

    [DataField(required: true)]
    public ProtoId<ToolQualityPrototype> ModuleExtractionMethod;

    [DataField(required: true)]
    public TimeSpan ModuleRemovalDelay;

    [DataField]
    public EntityWhitelist? ModuleWhitelist;

    [DataField]
    public EntityWhitelist? ModuleBlacklist;

    [DataField] public LocId InsertModuleVerbText = "modularhud-verb-insert-module";
    [DataField] public LocId InsertModuleVerbMessage = "modularhud-verb-insert-module-message";
    [DataField] public LocId ModuleFailsRequirementsErrorText = "modularhud-verb-insert-module-error-fails-requirements";
    [DataField] public LocId ModuleSlutsFullErrorText = "modularhud-verb-insert-module-error-slots-full";

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

[RegisterComponent, NetworkedComponent, Access(typeof(ModularHudSystem))]
public sealed partial class ModularHudVisualsComponent : Component
{
    [DataField(required: true)]
    public Dictionary<ModularHudVisuals, Color> DefaultVisuals = default!;

    [DataField]
    public Dictionary<ModularHudVisualKeys, string> LayerMap = [];

    [DataField]
    public List<string> SpeciesWithDifferentClothing = [];

    [DataField]
    public ModularHudVisualsExcludedLayers InhandExcludedLayers;

    [DataField]
    public ModularHudVisualsExcludedLayers EquippedExcludedLayers;

    [DataField]
    public string? FoldedLayerSuffix = null;

    [DataField]
    public Dictionary<ModularHudVisuals, ModularHudModuleColor> InnateVisuals = new();
}

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

/// Key for appearance data and marking un/folded sprite layers.
[Serializable, NetSerializable]
public enum FoldableModularHudVisuals : byte
{
    Key,
    LayerFolded,
    LayerUnfolded,
}

[RegisterComponent, NetworkedComponent, Access(typeof(ModularHudSystem))]
public sealed partial class ModularHudModuleComponent : Component
{
    [DataField]
    public Dictionary<ModularHudVisuals, ModularHudModuleColor> Visuals = new();
}

[DataRecord, Serializable, NetSerializable]
public readonly partial record struct ModularHudModuleColor(Color Color, int Priority = 0)
    : IComparable<ModularHudModuleColor>
{
    public int CompareTo(ModularHudModuleColor other)
    {
        return Priority.CompareTo(other.Priority);
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
    Accent,
    Lens,
    Specular,
    LensAccentMinor,
    LensAccentMajor,
}
