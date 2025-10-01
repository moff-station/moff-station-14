using Content.Shared._Moffstation.ClockCult.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Moffstation.ClockCult;

public sealed class ClockworkCultSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClockworkCultComponent, GetStatusIconsEvent>(OnClockCultGetIcons);
    }

    private void OnClockCultGetIcons(Entity<ClockworkCultComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
