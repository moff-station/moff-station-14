using Content.Shared.EntityEffects;

namespace Content.Shared._Moffstation;

// This system operates on `MetaDataComponent` because there's no `PointLightComponent` at the shared level.
public sealed partial class SetLightEnabledEntityEffectSystem : EntityEffectSystem<MetaDataComponent, SetLightEnabled>
{
    [Dependency] private SharedPointLightSystem _pointLight = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<SetLightEnabled> args)
    {
        SharedPointLightComponent? lightComp = null;
        if (!_pointLight.ResolveLight(entity, ref lightComp))
            return;

        _pointLight.SetEnabled(entity, args.Effect.Enabled, lightComp);
    }
}

/// An effect which enables and disables lights on the affected entity.
public sealed partial class SetLightEnabled : EntityEffectBase<SetLightEnabled>
{
    [DataField(required: true)]
    public bool Enabled;
}
