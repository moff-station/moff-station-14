using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.CartridgeLoader;

[Serializable, NetSerializable]
public sealed class TicketMasterPrintMessage(string author, string recipient, string message)
    : BoundUserInterfaceMessage
{
    public string Author = author;
    public string Recipient = recipient;
    public string Message = message;
}
