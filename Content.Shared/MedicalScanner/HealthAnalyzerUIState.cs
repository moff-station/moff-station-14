using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner;

/// <summary>
/// Contains the current state of a health analyzer control. Used for the health analyzer and cryo pod.
/// </summary>
[Serializable, NetSerializable]
public sealed class HealthAnalyzerUiState : BoundUserInterfaceState
{
    public readonly NetEntity? TargetEntity;
    public float Temperature;
    public float BloodLevel;
    public bool ScanMode;
    public bool Bleeding;
    public bool Unrevivable;
    public bool Uncloneable; //Moffstation change - uncloneable (duh)

    public HealthAnalyzerUiState() {}

    public HealthAnalyzerUiState(NetEntity? targetEntity, float temperature, float bloodLevel, bool scanMode, bool bleeding, bool unrevivable, bool uncloneable) //Moffstation change - uncloneable
    {
        TargetEntity = targetEntity;
        Temperature = temperature;
        BloodLevel = bloodLevel;
        ScanMode = scanMode;
        Bleeding = bleeding;
        Unrevivable = unrevivable;
        Uncloneable = uncloneable; //Moffstation change - uncloneable
    }
}
