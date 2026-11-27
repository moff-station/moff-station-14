using Content.Shared._Harmony.BloodBrothers.Components;
using Content.Shared._Harmony.BloodBrothers.EntitySystems;
using Content.Shared._Moffstation.BloodBrothers.Components;
using Content.Shared.Antag;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Harmony.BloodBrothers.EntitySystems;

public sealed partial class ClientBloodBrotherSystem : BloodBrotherSystem
{
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    [SubscribeLocalEvent]
    private void OnBloodBrotherGetIcons(Entity<BloodBrotherComponent> entity, ref GetStatusIconsEvent args)
    {
        if (_playerManager.LocalSession?.AttachedEntity is { } playerEntity &&
            !HasComp<ShowAntagIconsComponent>(playerEntity) &&
            entity.Owner != playerEntity &&
            entity.Comp.Brother != playerEntity)
            return;

        if (_prototypeManager.TryIndex(entity.Comp.BloodBrotherIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    [SubscribeLocalEvent]
    private void OnConvertibleGetIcons(Entity<BloodBrotherConvertibleComponent> entity, ref GetStatusIconsEvent args)
    {
        if (_prototypeManager.Resolve(entity.Comp.Icon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
