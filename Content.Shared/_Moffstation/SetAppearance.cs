using Content.Shared.EntityEffects;

namespace Content.Shared._Moffstation;

public sealed partial class SetAppearanceEntityEffectSystem : EntityEffectSystem<AppearanceComponent, SetAppearance>
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    protected override void Effect(Entity<AppearanceComponent> entity, ref EntityEffectEvent<SetAppearance> args)
    {
        foreach (var (key, value) in args.Effect.Values)
        {
            _appearance.SetData(entity, key, value, entity);
        }
    }
}

/// An effect which appends <see cref="AppearanceComponent"/> data from <see cref="Values"/>.
public sealed partial class SetAppearance : EntityEffectBase<SetAppearance>
{
    [DataField(required: true)]
    public Dictionary<Enum, string> Values = new();
}
