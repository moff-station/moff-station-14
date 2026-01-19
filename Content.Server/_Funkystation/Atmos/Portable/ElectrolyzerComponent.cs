// Funky atmos - /tg/ gases
using Robust.Shared.Audio;

namespace Content.Server._Funkystation.Atmos.Portable;

[RegisterComponent]
public sealed partial class ElectrolyzerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float CurrentFuel { get; set; } = 0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float PlasmaFuelConversion { get; set; } = 25000f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float UraniumFuelConversion { get; set; } = 150000f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsPowered { get; set; } = false;

    [DataField("onSound")]
    public SoundSpecifier? OnSound;
}
