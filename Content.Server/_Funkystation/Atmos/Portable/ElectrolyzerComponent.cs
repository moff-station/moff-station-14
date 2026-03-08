// Funky atmos - /tg/ gases
using Robust.Shared.Audio;

namespace Content.Server._Funkystation.Atmos.Portable;

[RegisterComponent]
public sealed partial class ElectrolyzerComponent : Component
{
    [DataField]
    public float CurrentFuel { get; set; }

    [DataField]
    public float PlasmaFuelConversion { get; set; } = 200000f;

    [DataField]
    public float UraniumFuelConversion { get; set; } = 1000000f;

    [DataField]
    public bool IsPowered { get; set; }

    [DataField]
    public SoundSpecifier? OnSound;
}
