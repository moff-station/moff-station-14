using Content.Shared._Moffstation.Extensions;
using Content.Shared.EntityEffects;
using Content.Shared.Light;
using Content.Shared.Light.Components;

namespace Content.Shared._Moffstation;

// This system operates on `MetaDataComponent` because there's no `PointLightComponent` at the shared level.
public sealed partial class SetLightToColorEntityEffectSystem :
    EntityEffectSystem<MetaDataComponent, SetLightColor>
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPointLightSystem _pointLight = default!;
    [Dependency] private SharedRgbLightControllerSystem _rgbLightController = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<SetLightColor> args)
    {
        SharedPointLightComponent? lightComp = null;
        if (!_pointLight.ResolveLight(entity, ref lightComp))
            return;

        if (args.Effect.Rgb is { } rgb)
        {
            var rgbComp = EnsureComp<RgbLightControllerComponent>(entity);
            _rgbLightController.SetCycleRate(entity, rgb.CycleRate, rgbComp);
            _rgbLightController.SetLayers(entity, rgb.AffectedLayers, rgbComp);
            return;
        }

        RemCompDeferred<RgbLightControllerComponent>(entity);
        if (args.Effect.AppearanceValue is { } key)
        {
            if (_appearance.TryGetData<Color>(entity, key, out var appearanceColor) ||
                _appearance.TryGetData<string>(entity, key, out var appearanceColorString) &&
                Color.TryParse(appearanceColorString, out appearanceColor))
            {
                _pointLight.SetColor(entity, appearanceColor, lightComp);
            }

            return;
        }

        if (args.Effect.Color is { } color)
        {
            _pointLight.SetColor(entity, color, lightComp);
            return;
        }

        this.AssertOrLogError($"No valid effect for {nameof(SetLightColor)}");
    }
}

/// An effect which sets the color of lights on the affected entity, as by <see cref="SharedPointLightSystem"/>
public sealed partial class SetLightColor : EntityEffectBase<SetLightColor>
{
    /// When set, sets lights to this color.
    [DataField] public Color? Color;

    /// When set and <see cref="Color"/> is not set, sets the color of lights to the <see cref="AppearanceComponent"/>
    /// data keyed by this key. This supports either <see cref="Color"/> objects or anything parseable by
    /// <see cref="Color.TryParse"/>.
    [DataField] public Enum? AppearanceValue;

    /// When set and <see cref="Color"/> and <see cref="AppearanceValue"/> are not set, sets lights to be controlled by
    /// a new <see cref="RgbLightControllerComponent"/> with parameters specified by this value.
    [DataField] public SetLightColorRgb? Rgb;
}

[DataRecord]
public partial record struct SetLightColorRgb()
{
    public float CycleRate = 1f;
    public List<int>? AffectedLayers = null;
}
