using Content.Shared.Hands.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Moffstation.Visuals;

/// This component is analogous to <see cref="GenericVisualizerComponent"/>, but extended to inhand, clothing and
/// storage visuals.
[RegisterComponent, Access(typeof(GenericVisualizerExtendedSystem))]
public sealed partial class GenericVisualizerExtendedComponent : Component
{
    /// <see cref="GenericVisualizerComponent.Visuals"/>, but for each hand.
    [DataField]
    public Dictionary<HandLocation, Dictionary<Enum, Dictionary<string, Dictionary<string, PrototypeLayerData>>>>
        InhandVisuals = new();

    /// <see cref="GenericVisualizerComponent.Visuals"/>, but for each clothing slot.
    [DataField]
    public Dictionary<string, Dictionary<Enum, Dictionary<string, Dictionary<string, PrototypeLayerData>>>>
        ClothingVisuals = new();

    /// <see cref="GenericVisualizerComponent.Visuals"/>, but for storage visuals.
    [DataField]
    public Dictionary<Enum, Dictionary<string, Dictionary<string, PrototypeLayerData>>> StoredVisuals = new();
}
