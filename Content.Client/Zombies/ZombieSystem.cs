using System.Linq;
using Content.Shared._Starlight.CollectiveMind; // Moffstation - Zombies not getting added to their Hivemind
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Zombies;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
namespace Content.Client.Zombies;

public sealed class ZombieSystem : SharedZombieSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly CollectiveMindUpdateSystem _collectiveMindUpdateSystem = default!; // Moffstation - Zombies not getting added to their Hivemind

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZombieComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ZombieComponent, GetStatusIconsEvent>(GetZombieIcon);
        SubscribeLocalEvent<InitialInfectedComponent, GetStatusIconsEvent>(GetInitialInfectedIcon);
    }

    private void GetZombieIcon(Entity<ZombieComponent> ent, ref GetStatusIconsEvent args)
    {
        var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }

    private void GetInitialInfectedIcon(Entity<InitialInfectedComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<ZombieComponent>(ent))
            return;

        var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }

    private void OnStartup(EntityUid uid, ZombieComponent component, ComponentStartup args)
    {
        // Moffstation - Begin - Update to give zombies their collective mind communication
        if (TryComp<CollectiveMindComponent>(uid, out var collective))
        {
            _collectiveMindUpdateSystem.UpdateCollectiveMind(uid, collective);
        }
        // Moffstation - End
        if (HasComp<HumanoidAppearanceComponent>(uid))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        for (var i = 0; i < sprite.AllLayers.Count(); i++)
        {
            _sprite.LayerSetColor((uid, sprite), i, component.SkinColor);
        }
    }
}
