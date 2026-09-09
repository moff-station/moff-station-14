using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.LogProbe;

[Serializable, NetSerializable]
public enum LogProbeUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class LogProbePrintBuiMessage : BoundUserInterfaceMessage;
