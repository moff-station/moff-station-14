using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.CartridgeLoader;

[Serializable, NetSerializable, DataRecord]
public sealed partial class TicketMasterTicketState(string authorName, string targetName, string description)
{
    public readonly string AuthorName = authorName;
    public readonly string TargetName = targetName;
    public readonly string Description = description;
}


[Serializable, NetSerializable]
public sealed class TicketMasterUiState()
{
    public List<TicketMasterTicketState> History = new();

}
