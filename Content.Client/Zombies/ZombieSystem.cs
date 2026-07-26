using System.Linq;
using Content.Shared._Starlight.CollectiveMind; // Moffstation - Zombies not getting added to their Hivemind
using Content.Shared.Body;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Zombies;
using Robust.Client.GameObjects;
namespace Content.Client.Zombies;

public sealed partial class ZombieSystem : SharedZombieSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private CollectiveMindUpdateSystem _collectiveMindUpdateSystem = default!; // Moffstation - Zombies not getting added to their Hivemind

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZombieComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ZombieComponent, GetStatusIconsEvent>(GetZombieIcon);
        SubscribeLocalEvent<InitialInfectedComponent, GetStatusIconsEvent>(GetInitialInfectedIcon);
    }

    private void GetZombieIcon(Entity<ZombieComponent> ent, ref GetStatusIconsEvent args)
    {
        var iconPrototype = ProtoMan.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }

    private void GetInitialInfectedIcon(Entity<InitialInfectedComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<ZombieComponent>(ent))
            return;

        var iconPrototype = ProtoMan.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }

    private void OnStartup(EntityUid uid, ZombieComponent component, ComponentStartup args)
    {
        _collectiveMindUpdateSystem.UpdateCollectiveMind(uid); // Moffstation - Give collective mind
        if (HasComp<VisualBodyComponent>(uid))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        for (var i = 0; i < sprite.AllLayers.Count(); i++)
        {
            _sprite.LayerSetColor((uid, sprite), i, component.SkinColor);
        }
    }
}
