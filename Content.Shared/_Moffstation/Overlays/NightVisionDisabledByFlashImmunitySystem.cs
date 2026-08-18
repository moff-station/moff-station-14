using Content.Shared.Flash;
using Content.Shared.NightVision;

namespace Content.Shared._Moffstation.Overlays;

/// This system implements the behavior of <see cref="NightVisionDisabledByFlashImmunityComponent"/>.
public sealed partial class NightVisionDisabledByFlashImmunitySystem : EntitySystem
{
    [Dependency] private SharedNightVisionSystem _nightVision = default!;

    [SubscribeLocalEvent]
    private void OnFlashImmunityChanged(
        Entity<NightVisionDisabledByFlashImmunityComponent> ent,
        ref FlashImmunityChangedEvent args
    )
    {
        _nightVision.SetEnabled(ent.Owner, !args.FlashImmune);
    }
}
