using Content.Shared._Moffstation.Atmos.Components;
using Content.Shared._Moffstation.Atmos.Visuals;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Atmos.EntitySystems;

public sealed partial class GasTankVisualsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public static readonly ProtoId<GasTankVisualStylePrototype> DefaultStyleId = "Default";
    public GasTankVisualStylePrototype DefaultStyle => _proto.Index(DefaultStyleId);

    public override void Initialize()
    {
        SubscribeLocalEvent<GasTankVisualsComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<GasTankVisualsComponent> entity, ref ComponentInit args)
    {
        if (!_proto.TryIndex(entity.Comp.InitialVisuals, out var visuals))
            return;

        SetTankVisuals((entity, entity.Comp, null), visuals);
    }

    public bool SetTankVisuals(
        Entity<GasTankVisualsComponent?, AppearanceComponent?> entity,
        GasTankVisuals visuals
    )
    {
        if (!Resolve(entity, ref entity.Comp1) ||
            !Resolve(entity, ref entity.Comp2) ||
            GetColorValues(visuals) is not { } colorValues)
            return false;

        entity.Comp1.Visuals = colorValues;
        _appearance.SetData(entity, GasTankVisualsLayers.Tank, colorValues.TankColor, entity.Comp2);
        _appearance.SetData((entity, entity.Comp2),
            GasTankVisualsLayers.StripeMiddle,
            colorValues.MiddleStripeColor);
        _appearance.SetData((entity, entity.Comp2), GasTankVisualsLayers.StripeLow, colorValues.LowerStripeColor);

        return true;
    }

    private GasTankColorValues? GetColorValues(GasTankVisuals visuals)
    {
        switch (visuals)
        {
            case GasTankVisuals.GasTankVisualsPrototype proto:
                _proto.TryIndex(proto.Prototype, out var style);
                return style?.ColorValues;
            case GasTankVisuals.GasTankVisualsColorValues values:
                return values;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
