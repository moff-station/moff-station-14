using Content.Shared._Moffstation.Atmos.Components;
using Content.Shared._Moffstation.Atmos.Visuals;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Atmos.EntitySystems;

/// <summary>
/// This system manages <see cref="GasTankVisualsComponent"/>s and everything involved in using both
/// <see cref="GasHolderVisualStylePrototype"/> and <see cref="GasTankColorValues"/>. The actual "translation" of the
/// contents of <see cref="GasTankVisualsComponent"/> is handled by
/// <see cref="Content.Client._Moffstation.Atmos.Visualizers.GasTankVisualizerSystem"/>.
/// </summary>
public sealed partial class GasHolderVisualsSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedItemSystem _item = default!;

    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery;

    /// <summary>
    /// <see cref="GasHolderVisualStylePrototype.DefaultId"/>, but resolved to an actual object.
    /// </summary>
    public GasHolderVisualStylePrototype DefaultStyle => ProtoMan.Index(GasHolderVisualStylePrototype.DefaultId);

    [SubscribeLocalEvent]
    private void OnInit(Entity<GasTankVisualsComponent> entity, ref ComponentInit args) => OnInit(entity);

    [SubscribeLocalEvent]
    private void OnInit(Entity<GasCanVisualsComponent> entity, ref ComponentInit args) => OnInit(entity);

    private void OnInit<T>(Entity<T> entity) where T : Component, IGasHolderVisualsComponent
    {
        // Set the color values to the specified prototype on component init.
        if (!ProtoMan.Resolve(entity.Comp.InitialVisuals, out var visuals) ||
            !_appearanceQuery.TryComp(entity, out var appearance))
            return;

        TrySetTankVisuals<T>((entity, entity.Comp, appearance), visuals);
    }

    /// <summary>
    /// Attempts to set <paramref name="entity"/>'s visuals to <paramref name="visuals"/>, which will indirectly lead to
    /// the tank's sprite being updated. If <paramref name="entity"/> doesn't have <typeparamref name="T"/>
    /// or doesn't have <see cref="AppearanceComponent"/>, or if <paramref name="visuals"/> cannot be resolved to
    /// <see cref="GasTankColorValues"/>, returns false; returns true otherwise.
    /// </summary>
    public bool TrySetTankVisuals<T>(
        Entity<T?, AppearanceComponent?> entity,
        GasHolderVisuals visuals
    ) where T : Component, IGasHolderVisualsComponent
    {
        if (!Resolve(entity, ref entity.Comp1) ||
            !_appearanceQuery.Resolve(entity, ref entity.Comp2) ||
            GetColorValues(visuals) is not { } colorValues)
            return false;

        entity.Comp1.Visuals = colorValues;
        Dirty(entity, entity.Comp1);

        var appearance = new Entity<AppearanceComponent?>(entity, entity.Comp2);
        _appearance.SetData(entity, GasHolderVisualsLayers.Tank, colorValues.TankColor, entity.Comp2);
        _appearance.SetOrRemoveData(appearance, GasHolderVisualsLayers.StripeMiddle, colorValues.MiddleStripeColor);
        _appearance.SetOrRemoveData(appearance, GasHolderVisualsLayers.StripeLow, colorValues.LowerStripeColor);

        return true;
    }

    private GasTankColorValues? GetColorValues(GasHolderVisuals visuals) => visuals switch
    {
        GasHolderVisuals.GasHolderVisualsPrototype proto => ProtoMan.Resolve(proto.Prototype, out var style)
            ? style.ColorValues
            : null,
        GasHolderVisuals.GasHolderVisualsColorValues values => values,
        _ => visuals.ThrowUnknownInheritor<GasHolderVisuals, GasTankColorValues?>(),
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
