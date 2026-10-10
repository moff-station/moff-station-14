using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.GaussFabricator;

[Serializable, NetSerializable]
public enum GaussFabricatorUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class GaussFabricatorBuiState(
    float receivedPower,
    float progress,
    float outputRate,
    float? temperature,
    float? pressure)
    : BoundUserInterfaceState
{
    public readonly float ReceivedPower = receivedPower;
    public readonly float Progress = progress;

    /// <summary>
    /// The rate of how many things are produced per minute.
    /// </summary>
    public readonly float OutputRate = outputRate;

    // Null when the fabricator isn't in a gas mixture.
    public readonly float? Temperature = temperature;
    public readonly float? Pressure = pressure;

    public override bool Equals(object? obj)
    {
        return obj is GaussFabricatorBuiState other
            && ReceivedPower.Equals(other.ReceivedPower)
            && Progress.Equals(other.Progress)
            && OutputRate.Equals(other.OutputRate)
            && Temperature.Equals(other.Temperature)
            && Pressure.Equals(other.Pressure);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            ReceivedPower,
            Progress,
            OutputRate,
            Temperature,
            Pressure);
    }
}

[Serializable, NetSerializable]
public sealed class GaussFabricatorAdjustDrawRateMessage(float delta) : BoundUserInterfaceMessage
{
    public readonly float Delta = delta;
}

[Serializable, NetSerializable]
public sealed class GaussFabricatorToggleMessage(bool on) : BoundUserInterfaceMessage
{
    public readonly bool On = on;
}
