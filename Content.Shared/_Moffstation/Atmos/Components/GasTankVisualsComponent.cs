using Content.Shared._Moffstation.Atmos.EntitySystems;
using Content.Shared._Moffstation.Atmos.Visuals;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Atmos.Components;

/// <summary>
/// This component stores the <see cref="GasTankColorValues"/> which describe how the owning entity should look
/// (assuming it's a Gas Tank).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(GasHolderVisualsSystem))]
public sealed partial class GasTankVisualsComponent : Component, IGasHolderVisualsComponent
{
    /// <summary>
    /// The current <see cref="GasTankColorValues"/> of this entity. Initialized by <see cref="InitialVisuals"/>.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public GasTankColorValues Visuals { get; set; } = new(default);

    /// <summary>
    /// Whether or not we should try to modify the stored sprite for this tank. Basically only for mergency tanks which
    /// are stored in inventory grids vertically.
    /// </summary>
    [DataField]
    public bool HasStoredSprite;

    /// <summary>
    /// Entity prototypes don't specify colors directly, and instead reference predefined
    /// <see cref="GasHolderVisualStylePrototype"/>s which contain the color values. This field is not used after the
    /// component is initialized.
    /// </summary>
    [DataField("visuals", readOnly: true)]
    public ProtoId<GasHolderVisualStylePrototype> InitialVisuals { get; set; } = GasHolderVisualStylePrototype.DefaultId;

    /// <summary>
    /// A list of layers which should not attempt to be shown when the gas tank is held in hand. This is provided
    /// because inhand sprites are small and can't have as much detail as is necessary to show all layers.
    /// </summary>
    [DataField]
    public IReadOnlyList<GasHolderVisualsLayers> ExcludedInhandLayers = [];

    /// <summary>
    /// A list of <see cref="InventoryComponent.SpeciesId">species IDs</see> which require different states be used for
    /// clothing.
    /// </summary>
    /// <remarks>Note that this isn't referring to SpeciesPrototype because animals (eg. Dog) don't actually
    /// get species prototypes.</remarks>
    [DataField]
    public IReadOnlyList<string> SpeciesWithDifferentClothing = [];
}

/// <summary>
/// This component stores the <see cref="GasTankColorValues"/> which describe how the owning entity should look
/// (assuming it's a Gas Tank).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(GasHolderVisualsSystem))]
public sealed partial class GasCanVisualsComponent : Component, IGasHolderVisualsComponent
{
    /// <summary>
    /// The current <see cref="GasTankColorValues"/> of this entity. Initialized by <see cref="InitialVisuals"/>.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public GasTankColorValues Visuals { get; set; } = new(default);

    /// <summary>
    /// Entity prototypes don't specify colors directly, and instead reference predefined
    /// <see cref="GasHolderVisualStylePrototype"/>s which contain the color values. This field is not used after the
    /// component is initialized.
    /// </summary>
    [DataField("visuals", readOnly: true)]
    public ProtoId<GasHolderVisualStylePrototype> InitialVisuals { get; set; } = GasHolderVisualStylePrototype.DefaultId;
}

public partial interface IGasHolderVisualsComponent
{
    GasTankColorValues Visuals { get; set; }
    ProtoId<GasHolderVisualStylePrototype> InitialVisuals { get; }
}
