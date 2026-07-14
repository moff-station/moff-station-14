using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Audio;

namespace Content.Server._Moffstation.Hellportal.Components;

[RegisterComponent]
public sealed partial class HellportalComponent : Component
{
    [DataField(required: true)]
    public EntityTableSelector BasicSpawnTable;

    [DataField]
    public float Accumulator = 0f;

    [DataField]
    public float SpawnCooldown = 30f;

    [DataField]
    public int MaxSpawns = 100;

    [DataField]
    public SoundSpecifier? Sound;

}
