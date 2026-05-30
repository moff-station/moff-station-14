using Content.Shared.Silicons.Laws;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Robotics.LawProgrammer;

[Serializable, NetSerializable]
public enum LawProgrammerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class LawProgrammerBuiState(string? name, List<SiliconLaw>? laws) : BoundUserInterfaceState
{
    public string? Name = name;
    public List<SiliconLaw>? Laws = laws;
}
