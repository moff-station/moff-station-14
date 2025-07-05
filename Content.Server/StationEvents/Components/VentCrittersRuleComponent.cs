﻿using Content.Server.StationEvents.Events;
using Content.Shared.Storage;

namespace Content.Server.StationEvents.Components;

// Moffstation - Start - Rename to use upstream functionality
[RegisterComponent, Access(typeof(UpstreamVentCrittersRule))]
public sealed partial class UpstreamVentCrittersRuleComponent : Component
// Moffstation - End
{
    [DataField("entries")]
    public List<EntitySpawnEntry> Entries = new();

    /// <summary>
    /// At least one special entry is guaranteed to spawn
    /// </summary>
    [DataField("specialEntries")]
    public List<EntitySpawnEntry> SpecialEntries = new();
}
