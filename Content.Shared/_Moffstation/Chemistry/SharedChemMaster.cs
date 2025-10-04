using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Chemistry;

public enum ChemMasterDrawSource
{
    Internal,
    External,
}

[Serializable, NetSerializable]
public sealed class ChemMasterOutputDrawSourceMessage(ChemMasterDrawSource drawSource) : BoundUserInterfaceMessage
{
    public readonly ChemMasterDrawSource DrawSource = drawSource;
}
