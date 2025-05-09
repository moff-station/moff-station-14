using Content.Server.Cargo.Systems;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Cargo.Components;

[RegisterComponent]
[Access(typeof(CargoSystem))]
public sealed partial class CargoPalletConsoleComponent : Component
{
    /// <summary>
    /// Moffstation - Makes a pallet send funds exclusively to one account
    /// </summary>
    [DataField("exclusiveAccount")]
    public ProtoId<CargoAccountPrototype>? ExclusiveAccount = null;
}
