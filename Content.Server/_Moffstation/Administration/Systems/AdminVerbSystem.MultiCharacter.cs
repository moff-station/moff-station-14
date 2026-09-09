using Content.Server._Moffstation.Station;
using Content.Server.Preferences.Managers;
using Content.Shared._Moffstation.Verbs;
using Content.Shared.Database;
using Content.Shared.Preferences;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private IServerPreferencesManager _moffPrefsManager = default!;
    [Dependency] private MoffCharacterPickerSystem _moffCharacterPicker = default!;

    /// <summary>
    /// One "spawn here" entry per character the player has, since there is no single selected one.
    /// </summary>
    private void AddMoffSpawnAsVerbs(GetVerbsEvent<Verb> args, ICommonSession target)
    {
        // OrNull, because this runs while building the verb list: GetPreferences throws for a user
        // whose preferences aren't cached, which would drop every other admin verb too.
        if (_moffPrefsManager.GetPreferencesOrNull(target.UserId) is not { } prefs)
            return;

        foreach (var (slot, profile) in prefs.Characters)
        {
            if (profile is not { } humanoid)
                continue;

            args.Verbs.Add(new Verb
            {
                Text = $"{slot}. {humanoid.Name}",
                Message = Loc.GetString("admin-player-actions-spawn-message"),
                Category = MoffVerbCategory.Spawn,
                Act = () =>
                {
                    if (!_transformSystem.TryGetMapOrGridCoordinates(args.Target, out var coords))
                    {
                        _popup.PopupEntity(Loc.GetString("admin-player-spawn-failed"), args.User, args.User);
                        return;
                    }

                    var stationUid = _stations.GetOwningStation(args.Target);
                    var mobUid = _spawning.SpawnPlayerMob(coords.Value, null, humanoid, stationUid);

                    if (_mindSystem.TryGetMind(args.Target, out var mindId, out var mindComp))
                        _mindSystem.TransferTo(mindId, mobUid, true, mind: mindComp);
                },
                ConfirmationPopup = true,
                Impact = LogImpact.High,
            });
        }
    }
}
