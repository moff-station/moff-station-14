using Content.Server._Moffstation.StationEvents.Events;
using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.Map;

namespace Content.Server._Moffstation.StationEvents.Components;

[RegisterComponent, Access(typeof(VentCrittersRule))]
public sealed partial class VentCrittersRuleComponent : Component
{
    [DataField("entries")]
    public List<EntitySpawnEntry> Entries = new();

    /// <summary>
    /// At least one special entry is guaranteed to spawn
    /// </summary>
    [DataField("specialEntries")]
    public List<EntitySpawnEntry> SpecialEntries = new();

    [ViewVariables]
    public int SpawnAttempts;

    /// <summary>
    /// The minimum amount of attempts something gets to spawn per player in the game. estimated number of spawns can be calculated with (SpawnAttempts * entryProb)
    /// </summary>
    [DataField]
    public int PlayerRatioSpawnsMin = 1;

    /// <summary>
    /// The maximum amount of attempts something gets to spawn per player in the game. estimated number of spawns can be calculated with (SpawnAttempts * entryProb)
    /// </summary>
    [DataField]
    public int PlayerRatioSpawnsMax = 3;

    /// <summary>
    /// Absolute maximum number of spawns that can occur, even if the spawns attempts permit for more
    /// </summary>
    [DataField]
    public int MaxSpawns = 10;

    [DataField]
    public TimeSpan PopupDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public SoundSpecifier? VentCreakNoise = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    [ViewVariables]
    public EntityUid? Vent;

    [ViewVariables]
    public EntityCoordinates? Coords;

    public TimeSpan NextPopup;

}
