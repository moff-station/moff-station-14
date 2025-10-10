using Content.Shared._Moffstation.Pirate.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Moffstation.Pirate;

public sealed class PirateSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PirateComponent, GetStatusIconsEvent>(OnPirateGetIcons);
    }

    private void OnPirateGetIcons(Entity<PirateComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
