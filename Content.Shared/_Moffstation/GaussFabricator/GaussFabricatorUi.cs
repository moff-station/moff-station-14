using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.GaussFabricator;

[Serializable, NetSerializable] public enum GaussFabricatorUiKey : byte { Key }

[Serializable, NetSerializable]
public sealed class GaussFabricatorBuiState(
    float receivedPower,
    float progress,
    float outputRate,
    float? temperature,
    float? pressure
) : BoundUserInterfaceState
{
    public readonly float ReceivedPower = receivedPower;
    public readonly float Progress = progress;

    /// How many items are produced per minute.
    public readonly float OutputRate = outputRate;

    // Null when the fabricator isn't in a gas mixture.
    public readonly float? Temperature = temperature;
    public readonly float? Pressure = pressure;
}

[Serializable, NetSerializable]
public sealed class GaussFabricatorSetDrawMessage(float draw) : BoundUserInterfaceMessage
{
    public readonly float Draw = draw;
}

[Serializable, NetSerializable]
public sealed class GaussFabricatorToggleMessage(bool on) : BoundUserInterfaceMessage
{
    public readonly bool On = on;
}
