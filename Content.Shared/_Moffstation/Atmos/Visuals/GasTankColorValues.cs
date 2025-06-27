using Content.Shared._Moffstation.Atmos.Components;
using Content.Shared._Moffstation.Atmos.EntitySystems;
using Content.Shared.Hands.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Atmos.Visuals;

/// <summary>
/// This enum is used to identify layers used for paintable gas tanks' sprites.
/// </summary>
[Serializable, NetSerializable]
public enum GasTankVisualsLayers : byte
{
    /// <summary>
    /// The base tank body.
    /// </summary>
    Tank,

    /// <summary>
    /// Everything that isn't the tank or stripes, ie. the valve and pressure gauge. This layer DOES NOT support coloring.
    /// </summary>
    Hardware,

    /// <summary>
    /// The stripe closer to the hardware. Generally larger than <see cref="StripeLow"/>.
    /// </summary>
    StripeMiddle,

    /// <summary>
    /// The stripe further from the hardware. Generally smaller than <see cref="StripeMiddle"/>.
    /// </summary>
    StripeLow,
}

public static class GasTankVisualsLayersExtensions
{
    public static readonly GasTankVisualsLayers[] ModifiableLayers =
        [GasTankVisualsLayers.Tank, GasTankVisualsLayers.StripeMiddle, GasTankVisualsLayers.StripeLow];

    private static string? RsiState(this GasTankVisualsLayers layer)
    {
        return layer switch
        {
            GasTankVisualsLayers.Tank => "tank",
            GasTankVisualsLayers.StripeMiddle => "stripe-middle",
            GasTankVisualsLayers.StripeLow => "stripe-low",
            _ => null,
        };
    }

    public static string? RsiStateInHand(this GasTankVisualsLayers layer, HandLocation hand)
    {
        return layer.RsiState() is { } state ? $"inhand-{hand.ToString().ToLowerInvariant()}-{state}" : null;
    }

    public static string? RsiStateEquipped(this GasTankVisualsLayers layer, string inventorySlot, string? species)
    {
        if (layer.RsiState() is not { } state)
            return null;

        var slotStr = inventorySlot switch
        {
            "suitstorage" => "SUITSTORAGE",
            "belt" => "BELT",
            "back" => "BACKPACK",
        };
        var speciesSuffix = species != null ? $"-{species.ToLowerInvariant()}" : "";
        return $"equipped-{slotStr}-{state}{speciesSuffix}";
    }
}

/// <summary>
/// This struct holds the actual <see cref="Color"/>s that are used to describe how a gas tank's sprite should look.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public partial record struct GasTankColorValues
{
    /// <summary>
    /// Color of <see cref="GasTankVisualsLayers.Tank"/>.
    /// </summary>
    [DataField(required: true)]
    public Color TankColor;

    /// <summary>
    /// Color of <see cref="GasTankVisualsLayers.StripeMiddle"/>.
    /// </summary>
    [DataField]
    public Color? MiddleStripeColor = null;

    /// <summary>
    /// Color of <see cref="GasTankVisualsLayers.StripeLow"/>.
    /// </summary>
    [DataField]
    public Color? LowerStripeColor = null;

    public GasTankColorValues(Color tankColor, Color? middleStripeColor = null, Color? lowerStripeColor = null)
    {
        TankColor = tankColor;
        MiddleStripeColor = middleStripeColor;
        LowerStripeColor = lowerStripeColor;
    }

    public static implicit operator GasTankColorValues((Color, Color?, Color?) values)
    {
        return new GasTankColorValues(values.Item1, values.Item2, values.Item3);
    }
}

/// <summary>
/// This prototype is used to predefine named <see cref="GasTankColorValues"/>. For example, there are prototypes for
/// oxygen and nitrogen tank color styles.
/// </summary>
[Prototype]
public sealed partial class GasTankVisualStylePrototype : IPrototype
{
    /// <summary>
    /// The default style to use if something blows up in the implementation, or if
    /// <see cref="GasTankVisualsComponent.InitialVisuals"/> is not set in the prototype definition.
    /// </summary>
    public static readonly ProtoId<GasTankVisualStylePrototype> DefaultId = "Default";

    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name of the gas or style (eg. "Air Mix") which this style denotes.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// The actual <see cref="ColorValues"/> this prototype specifies.
    /// </summary>
    [DataField]
    public GasTankColorValues ColorValues = new(default);

    public static implicit operator GasTankColorValues(GasTankVisualStylePrototype proto) => proto.ColorValues;
}

/// <summary>
/// A "sum type" which is either a <see cref="GasTankColorValues"/> or a <see cref="GasTankVisualStylePrototype"/> (via
/// subtypes <see cref="GasTankVisualsColorValues"/> and <see cref="GasTankVisualsPrototype"/> respectively). Those
/// types can implicitly convert into this one. This type can be converted into <see cref="GasTankColorValues"/> via
/// <see cref="GasTankVisualsSystem.GetColorValues"/> (but that should only happen internally to that system).
/// </summary>
[DataDefinition, ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class GasTankVisuals
{
    private GasTankVisuals()
    {
        // A private constructor here makes it impossible for anything but nested types to extend this type, effectively
        // sealing its inheritance.
    }

    public static implicit operator GasTankVisuals(GasTankVisualStylePrototype proto) =>
        new GasTankVisualsPrototype { Prototype = proto };

    public static implicit operator GasTankVisuals(GasTankColorValues values) =>
        new GasTankVisualsColorValues { Values = values };

    /// <summary>
    /// A <see cref="GasTankVisuals"/> which contains a <see cref="GasTankVisualStylePrototype"/>, <see cref="Prototype"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed partial class GasTankVisualsPrototype : GasTankVisuals
    {
        [DataField("id")]
        public ProtoId<GasTankVisualStylePrototype> Prototype;

        public override string ToString() => $"{GetType().Name}({nameof(Prototype)}={Prototype.Id})";
    }

    /// <summary>
    /// A <see cref="GasTankVisuals"/> which contains <see cref="GasTankColorValues"/>, <see cref="Values"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed partial class GasTankVisualsColorValues : GasTankVisuals
    {
        [DataField]
        public GasTankColorValues Values;

        public static implicit operator GasTankColorValues(GasTankVisualsColorValues values) => values.Values;

        public override string ToString() => $"{GetType().Name}({nameof(Values)}={Values})";
    }
}
