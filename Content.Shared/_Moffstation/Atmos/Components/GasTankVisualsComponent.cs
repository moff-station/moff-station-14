using Content.Shared._Moffstation.Atmos.EntitySystems;
using Content.Shared._Moffstation.Atmos.Visuals;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Atmos.Components;

/// <summary>
/// This component stores the <see cref="GasTankColorValues"/> which describe how the owning entity should look
/// (assuming it's a Gas Tank).
/// </summary>
[RegisterComponent, AutoGenerateComponentState, Access(typeof(GasTankVisualsSystem))]
public sealed partial class GasTankVisualsComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public GasTankColorValues Visuals = new(default);

    /// <summary>
    /// Entity prototypes don't specify colors directly, and instead reference predefined
    /// <see cref="GasTankVisualStylePrototype"/>s which contain the color values. This field is not used after the
    /// component is initialized.
    /// </summary>
    [DataField("visuals", readOnly: true)]
    public ProtoId<GasTankVisualStylePrototype> InitialVisuals = GasTankVisualStylePrototype.DefaultId;

    /// <summary>
    /// A list of layers which should not attempt to be shown when the gas tank is held in hand. This is provided
    /// because inhand sprites are small and can't have as much detail as is necessary to show all layers.
    /// </summary>
    [DataField("excludedInhandLayers")]
    public List<GasTankVisualsLayers> ExcludedInhandLayersData = [];

    /// <summary>
    /// A list of <see cref="InventoryComponent.SpeciesId">species IDs</see> which require different states be used for
    /// clothing.
    /// </summary>
    /// <remarks>Note that this isn't referring to SpeciesPrototype because animals (eg. Dog) don't actually
    /// get species prototypes.</remarks>
    [DataField("speciesWithDifferentClothing")]
    public List<string> SpeciesWithDifferentClothingData = [];

    /// <summary>
    /// Readonly public accessor of <see cref="ExcludedInhandLayersData"/>.
    /// </summary>
    [Access(Other = AccessPermissions.ReadExecute)]
    public IReadOnlyList<GasTankVisualsLayers> ExcludedInhandLayers => ExcludedInhandLayersData;

    /// <summary>
    /// Readonly public accessor of <see cref="SpeciesWithDifferentClothingData"/>.
    /// </summary>
    [Access(Other = AccessPermissions.ReadExecute)]
    public IReadOnlyList<string> SpeciesWithDifferentClothing => SpeciesWithDifferentClothingData;
}
