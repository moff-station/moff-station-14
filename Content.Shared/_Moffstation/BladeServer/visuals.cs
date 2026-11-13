using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.BladeServer;

/// <summary>
/// This enum is used to identify dynamically added layers on the BladeServer sprite.
/// </summary>
[Serializable, NetSerializable]
public enum BladeServerVisuals : byte
{
    StripeLayer,
    StripeColor,
}

/// <summary>
/// This enum is used to identify dynamically added layers on the BladeServerRack sprite.
/// </summary>
[Serializable, NetSerializable]
public enum BladeServerRackVisuals : byte
{
    SlotsKey,
}

/// <summary>
/// This data class holds the visual state of BladeServers contained within a BladeServerRack, used to specify how
/// dynamically added layers on the latter represent the former. This is used as data in <see cref="AppearanceComponent"/>
/// with <see cref="BladeServerRackVisuals.SlotsKey"/>.
/// </summary>
/// <param name="StripeColor">The color of the stripe on a particular Blade Server. Corresponds to <see cref="BladeServerVisuals.StripeColor"/>.</param>
/// <param name="Powered">Whether or not this BladeServer is powered.</param>
/// <param name="Offset">The offset for this particular BladeServer's slot. This is used to visually offset each blade.</param>
[Serializable, NetSerializable]
public readonly record struct BladeServerRackSlotVisualData(Color? StripeColor, bool Powered, Vector2 Offset)
{
    /// This class holds <see cref="BladeServerRackSlotVisualData"/> for ALL BladeServers in a BladeServerRack.
    [Serializable, NetSerializable]
    public readonly record struct Group(ImmutableArray<BladeServerRackSlotVisualData?> Slots);
}
