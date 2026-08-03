using Content.Server._Moffstation.Storage.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Storage.Components;

/// <summary>
/// <see cref="SpawnOnNextOpenSystem"/>
/// </summary>
[RegisterComponent, Access(typeof(SpawnOnNextOpenSystem))]
public sealed partial class SpawnOnNextOpenComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> Entities = [];
}
