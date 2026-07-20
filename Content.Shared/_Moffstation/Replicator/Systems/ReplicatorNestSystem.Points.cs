using System.Linq;
using Content.Shared._Moffstation.Replicator.Components;
using Content.Shared.Audio;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Replicator.Systems;

// This part has all of the point-calculation and reaction specifics.
public abstract partial class SharedReplicatorNestSystem
{
    [Dependency] private IGameTiming _timing = default!;

    private void AddPoints(Entity<ReplicatorNestComponent> ent, EntityUid consumed)
    {
        var sizePoints = 0;
        var spawnPoints = 0;
        foreach (var (whitelist, blacklist, size, spawn, name, special) in ent.Comp.PointEntries)
        {
            if (!_whitelist.CheckBoth(consumed, blacklist, whitelist))
                continue;

            if (special)
            {
                var ev = new ReplicatorNestSpecialPointCalculationEvent();
                RaiseLocalEvent(consumed, ref ev);
                sizePoints += ev.SizePoints;
                spawnPoints += ev.SpawningPoints;
            }

            sizePoints += CalculatePoints(size);
            spawnPoints += CalculatePoints(spawn);
        }

        ent.Comp.SizePoints += sizePoints;
        var pointsEv = new ReplicatorNestSizePointsGainedEvent(sizePoints);
        RaiseLocalEvent(ent, ref pointsEv);
        ent.Comp.SpawnPoints += spawnPoints;

        // Only start accruing points for tile conversion at endgame level.
        if (ent.Comp.CurrentLevel >= ent.Comp.EndgameLevel)
        {
            ent.Comp.TileConversionPoints += sizePoints;
        }

        Dirty(ent);
        return;

        int CalculatePoints(ReplicatorNestPointAward? p)
        {
            if (p is null)
                return 0;

            var pointsFromLevel = (int)Math.Floor(p.ScaledWithLevel.Float() * ent.Comp.CurrentLevel);
            var pointsFromSpawnCost = (int)Math.Floor(p.ScaledWithSpawnCost.Float() * CurrentSpawnCost(ent));
            return p.Flat + pointsFromLevel + pointsFromSpawnCost;
        }
    }

    private static int CurrentNestUpgradeCost(Entity<ReplicatorNestComponent> ent)
    {
        var calcLevel = Math.Min(ent.Comp.CurrentLevel, ent.Comp.EndgameLevel);
        return ent.Comp.UpgradeCostPerLevel * calcLevel;
    }

    private void CheckSizePoints(Entity<ReplicatorNestComponent> ent)
    {
        // if we exceed the upgrade threshold after points are added,
        var currentNestUpgradeCost = CurrentNestUpgradeCost(ent);
        if (ent.Comp.SizePoints < currentNestUpgradeCost)
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        ent.Comp.SizePoints -= currentNestUpgradeCost;
        ent.Comp.CurrentLevel += 1;

        // Member replicators get to upgrade.
        var ev = new EnableReplicatorUpgradesEvent();
        foreach (var replicator in HiveLeader.GetMembers(ent.Owner))
        {
            RaiseLocalEvent(replicator, ref ev);
        }

        // Upgrade nest visuals
        if (ent.Comp.CurrentLevel <= ent.Comp.EndgameLevel)
        {
            var (targetLayer, targetLayerUnshaded) = ent.Comp.CurrentLevel switch
            {
                >= 3 => (ReplicatorNestVisuals.Level3, ReplicatorNestVisuals.Level3Unshaded),
                2 => (ReplicatorNestVisuals.Level2, ReplicatorNestVisuals.Level2Unshaded),
                _ => (ReplicatorNestVisuals.Level1, ReplicatorNestVisuals.Level1Unshaded),
            };
            _appearance.SetData(ent, ReplicatorNestVisualsKeys.Key, targetLayer);
            _appearance.SetData(ent, ReplicatorNestVisualsKeys.KeyUnshaded, targetLayerUnshaded);
        }

        // Immediate feedback to replicators with a popup and sound.
        var levelUpPopup = Loc.TryGetString($"replicator-nest-level{ent.Comp.CurrentLevel}", out var localizedMsg)
            ? localizedMsg
            : Loc.GetString("replicator-nest-levelup");
        _popup.PopupPredicted(levelUpPopup, ent, ent);
        _audio.PlayPredicted(ent.Comp.LevelUpSound, ent, ent);

        // Run automatic announcements.
        foreach (var (level, announcement) in ent.Comp.Announcements.Where(it => it.Key <= ent.Comp.CurrentLevel)
                     .ToList())
        {
            // Remove the announcement so that we don't make it again later.
            ent.Comp.Announcements.Remove(level);
            if (_station.GetOwningStation(ent) is { } station)
            {
                _chat.DispatchStationAnnouncement(
                    station,
                    Loc.GetString(announcement),
                    playDefaultSound: true,
                    colorOverride: Color.Red
                );
            }
        }

        if (TryComp<AmbientSoundComponent>(ent, out var ambientComp))
        {
            _ambientSound.SetRange(ent, ambientComp.Range + 1, ambientComp);
        }

        Dirty(ent);
    }

    private int CurrentSpawnCost(Entity<ReplicatorNestComponent> ent)
    {
        var numMembers = HiveLeader.GetMembers(ent.Owner).Count();
        var numUnclaimedSpawners = ent.Comp.UnclaimedSpawners.Count;
        return ent.Comp.SpawnCostPerExistingSpawn * (numUnclaimedSpawners + numMembers);
    }

    private void CheckSpawnPoints(Entity<ReplicatorNestComponent> ent)
    {
        var currentSpawnCost = CurrentSpawnCost(ent);
        if (ent.Comp.SpawnPoints < currentSpawnCost)
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        ent.Comp.SpawnPoints -= currentSpawnCost;
        SpawnNew(ent);
        Dirty(ent);
    }

    private void CheckTileConversionPoints(Entity<ReplicatorNestComponent> ent)
    {
        // Only convert tiles if we're as big as we can get.
        if (ent.Comp.CurrentLevel < ent.Comp.EndgameLevel ||
            ent.Comp.TileConversionPoints < ent.Comp.TileConversionCost)
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        ent.Comp.TileConversionPoints -= ent.Comp.TileConversionCost;
        var conversionRadius = ent.Comp.TileConversionRadius + ent.Comp.TileConversionIncrease * ent.Comp.CurrentLevel;
        ConvertTiles(ent, conversionRadius);
    }
}
