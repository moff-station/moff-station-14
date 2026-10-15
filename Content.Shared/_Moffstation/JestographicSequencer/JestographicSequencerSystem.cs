using System.Linq;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Sound.Components;
using Content.Shared.Timing.Components;
using Content.Shared.Timing.Systems;
using Content.Shared.Whitelist;
using Content.Shared.Wires;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.JestographicSequencer;

///<summary>
///Handles using the jestographic sequencer on things with an "AccessReaderComponent",
///swapping their granted accesses for denied ones and vice versa.
///It can also be used on cyborgs, replacing their laws with the sequencer's lawset (see SharedSiliconLawSystem.Jestographic).
///</summary>
public sealed class JestographicSequencerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedStationAiSystem _stationAi = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JestographicSequencerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<JestographicSequencerComponent, BeforeRangedInteractEvent>(OnBeforeRangedInteract);
    }

    //Runs before the target's own InteractUsing, so storage items (e.g. the pie cannon) don't not allow it to be used.
    private void OnBeforeRangedInteract(Entity<JestographicSequencerComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (TryApplySpecialInteraction(ent, target, args.User))
            args.Handled = true;
    }

    private void OnAfterInteract(Entity<JestographicSequencerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        //Borgs get their laws swapped with panel open, but a closed one gets its access swapped.
        var isBorg = HasComp<BorgBrainComponent>(target)
                     || HasComp<BorgChassisComponent>(target)
                     && TryComp<WiresPanelComponent>(target, out var panel)
                     && panel.Open;
        AccessReaderComponent? reader = null;
        Entity<AccessReaderComponent>? readerEnt = null;
        if (!isBorg)
        {
            if (!_accessReader.GetMainAccessReader(target, out readerEnt))
                return;

            reader = readerEnt.Value.Comp;
        }

        if (_charges.IsEmpty(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("jestographic-sequencer-no-charges"), args.User, args.User);
            return;
        }

        if (isBorg)
        {
            //You've just been jested. A charge is only spent if it succeeds.
            var jested = new GotJestedEvent(args.User, ent.Comp.Lawset);
            RaiseLocalEvent(target, ref jested);
            if (!jested.Handled)
                return;

            _charges.TryUseCharge(ent.Owner);
            _audio.PlayPredicted(ent.Comp.ReverseSound, target, args.User);
            _adminLogger.Add(LogType.Emag,
                LogImpact.High,
                $"{ToPrettyString(args.User):player} jested {ToPrettyString(target):target} using {ToPrettyString(ent):used}");

            args.Handled = true;
            return;
        }

        if (reader == null || readerEnt == null)
            return;

        //Empty access lists allow everyone, so can't be reversed. Door is bolted instead.
        if (reader.AccessLists.Count == 0 && reader.DenyTags.Count == 0)
        {
            if (!TryComp<DoorBoltComponent>(target, out var bolt)
                || !_door.TrySetBoltDown((target, bolt), true, args.User, true))
            {
                _popup.PopupEntity(
                    Loc.GetString("jestographic-sequencer-no-access", ("target", Identity.Entity(target, EntityManager))),
                    args.User,
                    args.User);
                return;
            }

            _charges.TryUseCharge(ent.Owner);

            //Bolting also cuts the AI off from the door.
            if (TryComp<StationAiWhitelistComponent>(target, out var aiWhitelist))
                _stationAi.SetWhitelistEnabled((target, aiWhitelist), false, announce: true);

            _audio.PlayPredicted(ent.Comp.ReverseSound, target, args.User);
            _popup.PopupEntity(
                Loc.GetString("jestographic-sequencer-bolted", ("target", Identity.Entity(target, EntityManager))),
                args.User,
                args.User,
                PopupType.Medium);

            _adminLogger.Add(LogType.Emag,
                LogImpact.High,
                $"{ToPrettyString(args.User):player} bolted {ToPrettyString(target):target} shut using {ToPrettyString(ent):used}");

            args.Handled = true;
            return;
        }

        //A door swapped with the every-access-level fallback below is restored instead of swapped again.
        var restoring = IsFallbackSwap(reader);

        //Everything that used to grant access now denies it
        HashSet<ProtoId<AccessLevelPrototype>> newDenyTags = restoring
            ? new()
            : reader.AccessLists.SelectMany(x => x).ToHashSet();

        //Everything that used to deny access now grants it.
        var newAccessLists = new List<HashSet<ProtoId<AccessLevelPrototype>>>();
        foreach (var denyTag in reader.DenyTags)
        {
            newAccessLists.Add([denyTag]);
        }

        //The saved original keeps grouped requirements (e.g. needing two accesses at once) intact.
        if (restoring
            && reader.AccessListsOriginal is { } original
            && original.SelectMany(x => x).ToHashSet().SetEquals(reader.DenyTags))
        {
            newAccessLists = original.Select(x => x.ToHashSet()).ToList();
        }

        //Cause this was a pain to figure out, when swapping, the access list becoming empty and the deny list filling would let people in with no or blank IDs, defeating the purpose of the item 
        //since people could just take off their ID to get into places. While techincally not 100% swapping all access, this makes sure that any door it is used on still requires an ID to use. 
        //However if the door had no accesses before, it will still default to bolting w/ no change at all.
        //This introduces a rare scenario where someone with an entirely blank ID could somehow not gain access to a room/item they didnt have prior, but I think it is worth it.  
        if (newAccessLists.Count == 0)
        {
            foreach (var level in _prototype.EnumeratePrototypes<AccessLevelPrototype>())
            {
                newAccessLists.Add([level.ID]);
            }
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

    //True if the reader's access lists are exactly one entry per access level, which is what a swap leaves on a door with no deny tags.
    //Checks that there is a deny tag, access list entries have exactly one level, no dupes or extras.
    private bool IsFallbackSwap(AccessReaderComponent reader)
    {
        if (reader.DenyTags.Count == 0 || reader.AccessLists.Any(list => list.Count != 1))
            return false;

        var levels = _prototype.EnumeratePrototypes<AccessLevelPrototype>()
            .Select(level => new ProtoId<AccessLevelPrototype>(level.ID))
            .ToHashSet();

        return reader.AccessLists.Count == levels.Count
               && reader.AccessLists.Select(list => list.First()).ToHashSet().SetEquals(levels);
    }

    /// <summary>
    ///If there is a special interaction, apply the matching modification.
    ///Thought this would be cleaner than how the emag does it, and so im not editing a ton of upstream files, however I get it is basically an ugly series of if else checks every time it is used.
    /// </summary>
    private bool TryApplySpecialInteraction(Entity<JestographicSequencerComponent> ent, EntityUid target, EntityUid user)
    {
        var targetProto = Prototype(target)?.ID;

        foreach (var interaction in _prototype.EnumeratePrototypes<JestographicInteractionPrototype>())
        {
            var matches = targetProto != null && interaction.Prototypes.Contains(targetProto)
                          || _whitelist.IsWhitelistPass(interaction.Whitelist, target);
            if (!matches)
                continue;

            if (_charges.IsEmpty(ent.Owner))
            {
                _popup.PopupEntity(Loc.GetString("jestographic-sequencer-no-charges"), user, user);
                return true;
            }

            //Modules inside a chassis can't be swapped.
            var refuse = interaction.ReplaceWith is { } replacement
                ? targetProto == replacement.Id || TryComp<BorgModuleComponent>(target, out var module) && module.Installed
                : !interaction.Repeatable && HasComp<JestographicInteractedComponent>(target);
            if (refuse)
            {
                _popup.PopupEntity(
                    Loc.GetString("jestographic-sequencer-already-jested", ("target", Identity.Entity(target, EntityManager))),
                    user,
                    user);
                return true;
            }

            var targetCoordinates = Transform(target).Coordinates;
            if (interaction.ReplaceWith is { } newProto)
            {
                PredictedSpawnNextToOrDrop(newProto, target);
                PredictedQueueDel(target);
            }
            else
            {
                if (!ApplyInteractionEffects(interaction, target, user))
                    return true;

                if (!interaction.Repeatable)
                    AddComp<JestographicInteractedComponent>(target);
            }

            _charges.TryUseCharge(ent.Owner);
            _audio.PlayPredicted(ent.Comp.ReverseSound, targetCoordinates, user);
            _popup.PopupEntity(
                Loc.GetString(interaction.Message, ("target", Identity.Entity(target, EntityManager))),
                user,
                user,
                PopupType.Medium);

            _adminLogger.Add(LogType.Emag,
                LogImpact.Medium,
                $"{ToPrettyString(user):player} jested {ToPrettyString(target):target} using {ToPrettyString(ent):used}");

            return true;
        }

        return false;
    }

    private bool ApplyInteractionEffects(JestographicInteractionPrototype interaction, EntityUid target, EntityUid user)
    {
        if (interaction.UseDelay is { } delay && HasComp<UseDelayComponent>(target))
            _useDelay.SetLength(target, delay);

        if (interaction.SpeedMultiplier is { } speed && TryComp<MovementSpeedModifierComponent>(target, out var move))
        {
            _speed.ChangeBaseSpeed(
                target,
                move.BaseWalkSpeed * speed,
                move.BaseSprintSpeed * speed,
                move.Acceleration,
                move);
        }

        if (interaction.SoundIntervalMultiplier is { } interval && TryComp<SpamEmitSoundComponent>(target, out var spam))
        {
            spam.MinInterval *= interval;
            spam.MaxInterval *= interval;
        }

        if (interaction.RecyclerOutfit is { } outfit)
            EnsureComp<JestographicRecyclerComponent>(target).Outfit = outfit;

        if (interaction.CutCameraWires)
        {
            var wireCut = new JestographicCameraWireCutEvent(user);
            RaiseLocalEvent(target, ref wireCut);
            if (!wireCut.Handled)
                return false;
        }

        return true;
    }
}
