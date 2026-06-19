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
        if ((ent.Comp.Whitelist != null || ent.Comp.Blacklist != null) &&
            _whitelist.IsWhitelistPassOrNull(ent.Comp.Whitelist, args.Target) &&
            _whitelist.IsWhitelistFail(ent.Comp.Blacklist, args.Target))
            return;

        args.Cancel();

        if (ent.Comp.PopupText is { } popup)
        {
            _popup.PopupEntity(Loc.GetString(popup), ent, ent);
        }
    }
}
