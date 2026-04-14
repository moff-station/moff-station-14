using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Silicons.StationAi;

[Serializable, NetSerializable]
public sealed class JumpToAiShellMessage(NetEntity shell) : BoundUserInterfaceMessage
{
    public readonly NetEntity Shell = shell;
}

[Serializable, NetSerializable]
public sealed class StartAiShellControlMessage(NetEntity shell) : BoundUserInterfaceMessage
{
    public readonly NetEntity Shell = shell;
}

[Serializable, NetSerializable]
public sealed class SelectAiShellMessage(NetEntity? shell) : BoundUserInterfaceMessage
{
    public readonly NetEntity? Shell = shell;
}

[Serializable, NetSerializable]
public enum ShellUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed partial class AiShellControllerBuiState(
    NetEntity? selectedShell,
    List<NetEntity> selectableShells
) : BoundUserInterfaceState
{
    public NetEntity? SelectedShell = selectedShell;
    public List<NetEntity> SelectableShells = selectableShells;
}
