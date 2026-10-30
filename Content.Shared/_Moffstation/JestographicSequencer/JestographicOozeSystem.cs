using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.JestographicSequencer;

public sealed partial class JestographicOozeSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedDoorSystem _door = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStationAiSystem _stationAi = default!;
    [Dependency] private TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> SoapTag = "Soap";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JestographicOozeComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<JestographicOozeComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<JestographicOozeComponent, JestographicCleanDoAfterEvent>(OnCleaned);
    }

    /// <summary>
    ///Coats the target in ooze, saving what is needed to undo the sequencer's changes. Call before changing the reader.
    /// </summary>
    public void Coat(EntityUid target, AccessReaderComponent? reader, bool bolted = false, bool aiDisabled = false)
    {
        var ooze = EnsureComp<JestographicOozeComponent>(target);
        if (reader != null)
        {
            ooze.AccessLists = reader.AccessLists.Select(list => list.ToHashSet()).ToList();
            ooze.DenyTags = reader.DenyTags.ToHashSet();
        }

        ooze.Bolted = bolted;
        ooze.AiDisabled = aiDisabled;
        Dirty(target, ooze);
    }

    /// <summary>
    ///Restores what the sequencer changed and removes the ooze.
    /// </summary>
    public void Revert(Entity<JestographicOozeComponent> ent, EntityUid? user)
    {
        if (ent.Comp.AccessLists is { } accessLists && _accessReader.GetMainAccessReader(ent, out var reader))
        {
            _accessReader.SetDenyTags(reader.Value, ent.Comp.DenyTags.ToHashSet());
            _accessReader.TrySetAccesses(reader.Value, accessLists.Select(list => list.ToHashSet()).ToList());
        }

        if (ent.Comp.Bolted && TryComp<DoorBoltComponent>(ent, out var bolt))
            _door.TrySetBoltDown((ent, bolt), false, user, true);

        if (ent.Comp.AiDisabled && TryComp<StationAiWhitelistComponent>(ent, out var aiWhitelist))
            _stationAi.SetWhitelistEnabled((ent, aiWhitelist), true);

        RemComp<JestographicOozeComponent>(ent);
    }

    private void OnExamined(Entity<JestographicOozeComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("jestographic-ooze-examine"));
    }

    private void OnInteractUsing(Entity<JestographicOozeComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_tag.HasTag(args.Used, SoapTag))
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.CleanTime, new JestographicCleanDoAfterEvent(), ent, ent, args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            CancelDuplicate = false,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupEntity(
            Loc.GetString("jestographic-ooze-cleaning", ("target", Identity.Entity(ent, EntityManager))),
            ent,
            args.User);
        args.Handled = true;
    }

    private void OnCleaned(Entity<JestographicOozeComponent> ent, ref JestographicCleanDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        Revert(ent, args.Args.User);
        _popup.PopupEntity(
            Loc.GetString("jestographic-ooze-cleaned", ("target", Identity.Entity(ent, EntityManager))),
            ent,
            args.Args.User);
        args.Handled = true;
    }
}
