using Robust.Client.GameObjects;

namespace Content.Client._Moffstation.Visuals;

/// The component causes sprite layers to have their colors set based on <see cref="AppearanceComponent"/> data whenever
/// that data is set.
[RegisterComponent]
public sealed partial class SynchronizeLayerColorToAppearanceComponent : Component
{
    /// The <see cref="SpriteComponent"/> layer identified by keys in this value are set to color values in
    /// <see cref="AppearanceComponent"/> data keyed by values in this component.
    [DataField] public Dictionary<string, Enum> Layers = new();

    /// <see cref="Layers"/>, but for clothing. The first key in this field is a clothing slot (eg. "Head"), and the
    /// values in this dictionary work like <see cref="Layers"/>.
    /// Note that complicated usecases which modify clothing layers in C# may set their layer names to things different
    /// from the layer names set in yaml!
    [DataField] public Dictionary<string, Dictionary<string, Enum>> ClothingLayers = new();

    // TODO Inhand and storage sprite color modulation when ItemSystem supports it.
}
