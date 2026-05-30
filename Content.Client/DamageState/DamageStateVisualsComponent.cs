using Content.Shared._Moffstation.DamageState;//Moffstation - Re-add Geras
using Content.Shared.Mobs;

namespace Content.Client.DamageState;

[RegisterComponent]
public sealed partial class DamageStateVisualsComponent : Component
{
    public int? OriginalDrawDepth;

    [DataField("states")] public Dictionary<MobState, Dictionary<DamageStateVisualLayers, string>> States = new();
}

//Moffstation - Re-add Geras (enum moved to Content.Shared._Moffstation.DamageState.DamageStateVisualLayers.cs to make it available to shared systems)
