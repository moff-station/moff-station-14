using Content.Client.UserInterface.Fragments;
using Content.Shared._Moffstation.CartridgeLoader;
using Content.Shared.CartridgeLoader;
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
        _fragment.OnTicketPrinted += (author, recipient, text) =>
        {
            PrintTicket(author, recipient, text, userInterface);
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        // TODO : treat if casted
    }

    private void PrintTicket(string officer, string offender, string text, BoundUserInterface userInterface)
    {
        var msg = new TicketMasterPrintMessage(officer, offender, text);
        userInterface.SendMessage(new CartridgeUiMessage(new TicketMasterPrintMessageEvent(new TicketMasterTicketState(officer, offender, text), false)));
    }
}
