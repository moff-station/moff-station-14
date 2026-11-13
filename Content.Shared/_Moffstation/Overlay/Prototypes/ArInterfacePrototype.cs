using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Overlay.Prototypes;

/// <summary>
/// This is a prototype for an augmented reality interface.
/// Used as a key to determine what entities will be displayed by ArInterfaceSystem client-side.
/// </summary>
[Prototype]
public sealed partial class ArInterfacePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;
}
