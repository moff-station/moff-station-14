using Content.Shared._Moffstation.Atmos.Components;
using Content.Shared._Moffstation.Atmos.Visuals;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Item;
using Robust.Shared.Containers;
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
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedItemSystem _item = default!;

    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery;
    [Dependency] private EntityQuery<GasTankVisualsComponent> _gasTankVisualsQuery;

    /// <summary>
    /// <see cref="GasTankVisualStylePrototype.DefaultId"/>, but resolved to an actual object.
    /// </summary>
    public GasTankVisualStylePrototype DefaultStyle => ProtoMan.Index(GasTankVisualStylePrototype.DefaultId);

    [SubscribeLocalEvent]
    private void OnInit(Entity<GasTankVisualsComponent> entity, ref ComponentInit args)
    {
        // Set the color values to the specified prototype on component init.
        if (!ProtoMan.Resolve(entity.Comp.InitialVisuals, out var visuals) ||
            !_appearanceQuery.TryComp(entity, out var appearance))
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
        if (!_gasTankVisualsQuery.Resolve(entity, ref entity.Comp1) ||
            !_appearanceQuery.Resolve(entity, ref entity.Comp2) ||
            GetColorValues(visuals) is not { } colorValues)
            return false;

        var appearance = new Entity<AppearanceComponent?>(entity, entity.Comp2);
        entity.Comp1.Visuals = colorValues;
        _appearance.SetData(entity, GasTankVisualsLayers.Tank, colorValues.TankColor, entity.Comp2);
        _appearance.SetOrRemoveData(appearance, GasTankVisualsLayers.StripeMiddle, colorValues.MiddleStripeColor);
        _appearance.SetOrRemoveData(appearance, GasTankVisualsLayers.StripeLow, colorValues.LowerStripeColor);

        return true;
    }

    private GasTankColorValues? GetColorValues(GasTankVisuals visuals) => visuals switch
    {
        GasTankVisuals.GasTankVisualsPrototype proto => ProtoMan.Resolve(proto.Prototype, out var style)
            ? style.ColorValues
            : null,
        GasTankVisuals.GasTankVisualsColorValues values => values,
        _ => visuals.ThrowUnknownInheritor<GasTankVisuals, GasTankColorValues?>(),
    };

    [SubscribeLocalEvent]
    private void OnEntGotInsertedIntoContainer(
        Entity<GasTankVisualsComponent> entity,
        ref EntGotInsertedIntoContainerMessage args
    )
    {
        // Update stored visuals.
        _item.VisualsChanged(entity);
    }
}
