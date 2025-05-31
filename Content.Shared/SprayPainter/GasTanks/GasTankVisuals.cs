using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SprayPainter.GasTanks;

[RegisterComponent, AutoGenerateComponentState, Access(typeof(GasTankPainterSystem))]
public sealed partial class GasTankVisualsComponent : Component
{
    [DataField(tag: "appearance"), AutoNetworkedField]
    public GasTankVisuals Visuals = new(default);

    [DataField("visuals", required: true)]
    public ProtoId<GasTankVisualStylePrototype> InitialVisuals;
}

[Serializable, NetSerializable, DataDefinition]
public partial record struct GasTankVisuals
{
    [DataField(required: true)]
    public Color TankColor;

    [DataField]
    public Color? MiddleStripeColor = null;

    [DataField]
    public Color? LowerStripeColor = null;

    public GasTankVisuals(Color tankColor, Color? middleStripeColor = null, Color? lowerStripeColor = null)
    {
        TankColor = tankColor;
        MiddleStripeColor = middleStripeColor;
        LowerStripeColor = lowerStripeColor;
    }

    public static implicit operator GasTankVisuals((Color, Color?, Color?) values)
    {
        return new GasTankVisuals(values.Item1, values.Item2, values.Item3);
    }
}

[Serializable, NetSerializable]
public enum GasTankVisualsLayers : byte
{
    Tank,
    Hardware,
    StripeMiddle,
    StripeLow,
}

[Prototype]
public sealed partial class GasTankVisualStylePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public GasTankVisuals Visuals = new(default);

    public static implicit operator GasTankVisuals(GasTankVisualStylePrototype proto) => proto.Visuals;
}
