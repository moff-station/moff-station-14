using System.Linq;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.JestographicSequencer;

///<summary>
///Handles using the jestographic sequencer on things with an "AccessReaderComponent",
///swapping their granted accesses for denied ones and vice versa.
///</summary>
public sealed class JestographicSequencerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    [LocalEventSubscription]
    private void OnAfterInteract(Entity<JestographicSequencerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!_accessReader.GetMainAccessReader(target, out var readerEnt))
            return;

        var reader = readerEnt.Value.Comp;

        //Cant revert access if door allows everyone already (no accesses). For now just says nah cant do it. TODO: figure out how to make a door have no access at all or make it bolt the door instead perhaps? Thinking on it.
        if (reader.AccessLists.Count == 0 && reader.DenyTags.Count == 0)
        {
            _popup.PopupEntity(
                Loc.GetString("jestographic-sequencer-no-access", ("target", Identity.Entity(target, EntityManager))),
                args.User,
                args.User);
            return;
        }

        if (_charges.IsEmpty(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("jestographic-sequencer-no-charges"), args.User, args.User);
            return;
        }

        //Everything that used to grant access now denies it
        var newDenyTags = reader.AccessLists.SelectMany(x => x).ToHashSet();

        //Everything that used to deny access now grants it.
        var newAccessLists = new List<HashSet<ProtoId<AccessLevelPrototype>>>();
        foreach (var denyTag in reader.DenyTags)
        {
            newAccessLists.Add([denyTag]);
        }

        _accessReader.SetDenyTags(readerEnt.Value, newDenyTags);
        _accessReader.TrySetAccesses(readerEnt.Value, newAccessLists);

        _charges.TryUseCharge(ent.Owner);

        _audio.PlayPredicted(ent.Comp.ReverseSound, target, args.User);
        _popup.PopupEntity(
            Loc.GetString("jestographic-sequencer-success", ("target", Identity.Entity(target, EntityManager))),
            args.User,
            args.User,
            PopupType.Medium);

        _adminLogger.Add(LogType.Emag,
            LogImpact.High,
            $"{ToPrettyString(args.User):player} reversed the accesses on {ToPrettyString(target):target} using {ToPrettyString(ent):used}");

        args.Handled = true;
    }
}
