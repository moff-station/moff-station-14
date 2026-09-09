using System.Linq;
using Content.Shared.EntityEffects;
using Content.Shared.Verbs;

namespace Content.Shared._Moffstation;

/// This system implements the behavior of <see cref="EffectiveAlternativeVerbsComponent"/>
public sealed partial class EffectiveAlternativeVerbsSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;

    [SubscribeLocalEvent]
    private void GetVerbs(Entity<EffectiveAlternativeVerbsComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var priority = 0;
        var user = args.User;
        args.Verbs.UnionWith(entity.Comp.Categories.SelectMany(category =>
        {
            var cat = new VerbCategory(category.Text, category.Icon?.ToString());
            return category.Options.Select(it => new AlternativeVerb
            {
                Text = Loc.GetString(it.Text),
                Icon = it.Icon,
                Category = cat,
                Priority = priority--,
                Act = () =>
                {
                    foreach (var effect in it.Effects)
                    {
                        if (effect.ApplyToUser)
                        {
                            _entityEffects.ApplyEffect(user, effect.Effect, user: user);
                        }
                        else
                        {
                            _entityEffects.ApplyEffect(entity, effect.Effect, user: user);
                        }
                    }
                },
            });
        }));
    }
}
