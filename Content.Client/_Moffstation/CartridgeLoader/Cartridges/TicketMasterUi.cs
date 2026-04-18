using Content.Client.UserInterface.Fragments;
using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.CartridgeLoader.Cartridges;

public sealed partial class TicketMasterUi : UIFragment
{
    private TicketMasterUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new TicketMasterUiFragment();
        _fragment.OnTicketPrinted += PrintTicket;
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        // TODO : treat if casted
    }

    private static void PrintTicket(string officer, string offender, string text)
    {

    }
}
