using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Trinary.Components;

[Serializable, NetSerializable]
public enum GasFilterUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class GasFilterToggleStatusMessage : BoundUserInterfaceMessage
{
    public bool Enabled { get; }

    public GasFilterToggleStatusMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class GasFilterChangeRateMessage : BoundUserInterfaceMessage
{
    public float Rate { get; }

    public GasFilterChangeRateMessage(float rate)
    {
        Rate = rate;
    }
}

[Serializable, NetSerializable]
    public sealed class GasFilterSelectGasMessage(Gas gas, bool filtered) : BoundUserInterfaceMessage // Moff - filter multiple gasses
{
    public readonly Gas Gas = gas; // Moff - filter multiple gasses; Gas is not nullable
    public readonly bool Filtered = filtered;  // Moff - filter multiple gasses
}
