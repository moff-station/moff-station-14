using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Atmos.Visuals;

[Serializable, NetSerializable]
public enum GasTankVisualsLayers : byte
{
    Tank,
    Hardware,
    StripeMiddle,
    StripeLow,
}

[Serializable, NetSerializable, DataDefinition]
public partial record struct GasTankColorValues
{
    [DataField(required: true)]
    public Color TankColor;

    [DataField]
    public Color? MiddleStripeColor = null;

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

[Prototype]
public sealed partial class GasTankVisualStylePrototype : IPrototype
{
    public static readonly ProtoId<GasTankVisualStylePrototype> DefaultId = "Default";

    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public GasTankColorValues ColorValues = new(default);

    public static implicit operator GasTankColorValues(GasTankVisualStylePrototype proto) => proto.ColorValues;
}

[DataDefinition, ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class GasTankVisuals
{
    private GasTankVisuals()
    {
        // Sealed abstract type.
    }

    public static implicit operator GasTankVisuals(GasTankVisualStylePrototype proto) =>
        new GasTankVisualsPrototype { Prototype = proto };

    public static implicit operator GasTankVisuals(GasTankColorValues values) =>
        new GasTankVisualsColorValues { Values = values };

    [Serializable, NetSerializable]
    public sealed partial class GasTankVisualsPrototype : GasTankVisuals
    {
        [DataField("id")]
        public ProtoId<GasTankVisualStylePrototype> Prototype;

        public override string ToString() => $"{GetType().Name}({nameof(Prototype)}={Prototype.Id})";
    }

    [Serializable, NetSerializable]
    public sealed partial class GasTankVisualsColorValues : GasTankVisuals
    {
        [DataField] public GasTankColorValues Values;

        public static implicit operator GasTankColorValues(GasTankVisualsColorValues values) => values.Values;

        public override string ToString() => $"{GetType().Name}({nameof(Values)}={Values})";
    }
}
