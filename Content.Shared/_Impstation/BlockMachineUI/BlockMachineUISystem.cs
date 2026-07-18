using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Whitelist;

namespace Content.Shared._Impstation.BlockMachineUI;

public sealed partial class SharedBlockMachineUISystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlockMachineUIComponent, UserOpenActivatableUIAttemptEvent>(OnUIOpenAttempt);
    }

    private void OnUIOpenAttempt(Entity<BlockMachineUIComponent> ent, ref UserOpenActivatableUIAttemptEvent args)
    {
        if (_whitelist.CheckBoth(args.Target, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        args.Cancel();

        if (ent.Comp.PopupText is { } popup)
        {
            _popup.PopupEntity(Loc.GetString(popup), ent, ent);
        }
    }
}
