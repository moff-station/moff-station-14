using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.CartridgeLoader;

[Serializable, NetSerializable]
public sealed class TicketMasterPrintMessageEvent(TicketMasterTicketState ticket, bool addToHistory)
    : CartridgeMessageEvent
{
    public readonly TicketMasterTicketState Ticket = ticket;
    public readonly bool AddToHistory = addToHistory;
}
