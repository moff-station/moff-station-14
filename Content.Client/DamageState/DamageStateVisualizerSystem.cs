//Moffstation - Re-add Geras - Begin
using Content.Shared._Moffstation.DamageState;
using Content.Shared.Body;
//Moffstation - End
using Content.Shared.Mobs;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.DamageState;

public sealed class DamageStateVisualizerSystem : VisualizerSystem<DamageStateVisualsComponent>
{

    //Moffstation - Re-add Geras - Begin
    [Dependency] private readonly BodySystem _bodySystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, AppearanceChangeEvent>(OnBodyAppearanceChange);
        SubscribeLocalEvent<DamageStateVisualsComponent, BodyRelayedEvent<AppearanceChangeEvent>>(OnAppearanceChange);
    }

    private void OnBodyAppearanceChange(Entity<BodyComponent> ent, ref AppearanceChangeEvent args)
    {
        _bodySystem.RelayEvent(ent, ref args);
    }

    private void OnAppearanceChange(Entity<DamageStateVisualsComponent> ent, ref BodyRelayedEvent<AppearanceChangeEvent> args)
    {
        if (!TryComp<AppearanceComponent>(ent.Owner, out var appearance))
            return;
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        var ev = new AppearanceChangeEvent{Component = appearance, AppearanceData = args.Args.AppearanceData, Sprite = sprite};
        OnAppearanceChange(ent.Owner, ent.Comp, ref ev);
    }
    //Moffstation - End

    protected override void OnAppearanceChange(EntityUid uid, DamageStateVisualsComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null || !AppearanceSystem.TryGetData<MobState>(uid, MobStateVisuals.State, out var data, args.Component))
        {
            return;
        }

        if (!component.States.TryGetValue(data, out var layers))
        {
            return;
        }

        // Brain no worky rn so this was just easier.
        foreach (var key in new[] { DamageStateVisualLayers.Base, DamageStateVisualLayers.BaseUnshaded })
        {
            if (!SpriteSystem.LayerMapTryGet((uid, sprite), key, out _, false)) continue;

            SpriteSystem.LayerSetVisible((uid, sprite), key, false);
        }

        foreach (var (key, state) in layers)
        {
            // Inheritance moment.
            if (!SpriteSystem.LayerMapTryGet((uid, sprite), key, out _, false)) continue;

            SpriteSystem.LayerSetVisible((uid, sprite), key, true);
            SpriteSystem.LayerSetRsiState((uid, sprite), key, state);

            //Moffstation - Re-add Geras - Begin
            //forcibly set the state value if the entity being updated is a VisualOrgan because it won't do it on its own
            if (HasComp<VisualOrganComponent>(uid))
            {
                var ev = new ForceUpdateOrganVisualsEvent { State = state };
                RaiseLocalEvent(uid, ref ev);
            }
            //Moffstation - End
        }

        // So they don't draw over mobs anymore
        if (data == MobState.Dead)
        {
            if (sprite.DrawDepth > (int)DrawDepth.DeadMobs)
            {
                component.OriginalDrawDepth = sprite.DrawDepth;
                SpriteSystem.SetDrawDepth((uid, sprite), (int)DrawDepth.DeadMobs);
            }
        }
        else if (component.OriginalDrawDepth != null)
        {
            SpriteSystem.SetDrawDepth((uid, sprite), component.OriginalDrawDepth.Value);
            component.OriginalDrawDepth = null;
        }
    }
}
