using Content.Shared._Moffstation.Atmos.Components;
using Content.Shared._Moffstation.Atmos.Visuals;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Atmos.EntitySystems;

/// <summary>
/// This system manages <see cref="GasTankVisualsComponent"/>s and everything involved in using both
/// <see cref="GasTankVisualStylePrototype"/> and <see cref="GasTankColorValues"/>. The actual "translation" of the
/// contents of <see cref="GasTankVisualsComponent"/> is handled by
/// <see cref="Content.Client._Moffstation.Atmos.Visualizers.GasTankVisualizerSystem"/>.
/// </summary>
public sealed partial class GasTankVisualsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    /// <summary>
    /// <see cref="GasTankVisualStylePrototype.DefaultId"/>, but resolved to an actual object.
    /// </summary>
    public GasTankVisualStylePrototype DefaultStyle => _proto.Index(GasTankVisualStylePrototype.DefaultId);

    public override void Initialize()
    {
        SubscribeLocalEvent<GasTankVisualsComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<GasTankVisualsComponent> entity, ref ComponentInit args)
    {
        // Set the color values to the specified prototype on component init.
        if (!_proto.TryIndex(entity.Comp.InitialVisuals, out var visuals) ||
            !TryComp<AppearanceComponent>(entity, out var appearance))
            return;

        TrySetTankVisuals((entity, entity.Comp, appearance), visuals);
    }

    /// <summary>
    /// Attempts to set <paramref name="entity"/>'s visuals to <paramref name="visuals"/>, which will indirectly lead to
    /// the tank's sprite being updated. If <paramref name="entity"/> doesn't have <see cref="GasTankVisualsComponent"/>
    /// or doesn't have <see cref="AppearanceComponent"/>, or if <paramref name="visuals"/> cannot be resolved to
    /// <see cref="GasTankColorValues"/>, returns false; returns true otherwise.
    /// </summary>
    public bool TrySetTankVisuals(
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
        SetOrRemoveAppearanceData((entity, entity.Comp2),
            GasTankVisualsLayers.StripeMiddle,
            colorValues.MiddleStripeColor);
        SetOrRemoveAppearanceData((entity, entity.Comp2), GasTankVisualsLayers.StripeLow, colorValues.LowerStripeColor);

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

    /// <summary>
    /// If <paramref name="value"/> is null, <see cref="SharedAppearanceSystem.RemoveData">removes</see>
    /// <paramref name="key"/> from <paramref name="entity"/>'s appearance data, otherwise
    /// <see cref="SharedAppearanceSystem.SetData">sets</see> it to <paramref name="value"/>.
    /// </summary>
    private void SetOrRemoveAppearanceData(Entity<AppearanceComponent?> entity, Enum key, object? value)
    {
        if (value is not null)
        {
            _appearance.SetData(entity, key, value, entity);
        }
        else
        {
            _appearance.RemoveData(entity, key, entity);
        }
    }
}
