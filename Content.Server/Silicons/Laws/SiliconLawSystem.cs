using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Overlays;
using Content.Shared.Roles;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.Silicons.Laws;

/// <inheritdoc/>
public sealed partial class SiliconLawSystem : SharedSiliconLawSystem
{
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _roles = default!;
    // [Dependency] private StationSystem _station = default!; // Moffstation - Now Unused
    // [Dependency] private UserInterfaceSystem _userInterface = default!; // Moffstation - Now Unused
    // [Dependency] private EmagSystem _emag = default!; // Moffstation - Now Unused
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;

    private static readonly ProtoId<SiliconLawsetPrototype> DefaultCrewLawset = "Crewsimov";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconLawBoundComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<SiliconLawBoundComponent, BeforeMindRemovedMessage>(OnMindRemoved);
    }

    private void OnMindAdded(Entity<SiliconLawBoundComponent> ent, ref MindAddedMessage args)
    {
        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        _adminLogger.Add(LogType.SiliconLaw, LogImpact.Low, $"{ent.Owner} laws at MindAdded are [{ent.Comp.Lawset?.LoggingString()}]");

        UpdateSiliconRoles(ent);

        var msg = Loc.GetString("laws-notify");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.FromHex("#5ed7aa"));

        if (!ent.Comp.Subverted)
            return;

        var modifedLawMsg = Loc.GetString("laws-notify-subverted");
        var modifiedLawWrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", modifedLawMsg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, modifedLawMsg, modifiedLawWrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.Red);
    }

    private void OnMindRemoved(Entity<SiliconLawBoundComponent> ent, ref BeforeMindRemovedMessage args)
    {
        _adminLogger.Add(LogType.SiliconLaw, LogImpact.Low, $"{ent.Owner} laws at MindAdded are [{ent.Comp.Lawset?.LoggingString()}]");

        if (args.TransferEntity is not { } owner)
            return;

        UpdateSiliconRoles(owner, args.Mind);
    }

    public override void NotifyLawsChanged(Entity<SiliconLawProviderComponent> ent, SoundSpecifier? cue = null)
    {
        base.NotifyLawsChanged(ent, cue);

        _adminLogger.Add(LogType.SiliconLaw, LogImpact.Low, $"{ent} laws changed to [{ent.Comp.Lawset?.LoggingString()}]");

        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        var msg = Loc.GetString("laws-update-notify");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        UpdateLawVersion(ent.Owner);
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.Red);

        if (cue != null && _mind.TryGetMind(ent, out var mindId, out _))
            _roles.MindPlaySound(mindId, cue);
    }

    /// <summary>
    /// Extract all the laws from a lawset's prototype ids.
    /// </summary>
    public SiliconLawset GetLawset(ProtoId<SiliconLawsetPrototype> lawset)
    {
        var proto = ProtoMan.Index(lawset);
        var laws = new SiliconLawset()
        {
            Laws = new List<SiliconLaw>(proto.Laws.Count)
        };
        foreach (var law in proto.Laws)
        {
            laws.Laws.Add(ProtoMan.Index<SiliconLawPrototype>(law).ShallowClone());
        }
        laws.ObeysTo = proto.ObeysTo;

        return laws;
    }

    /// <summary>
    /// Set the laws of a silicon entity while notifying the player.
    /// </summary>
    public void SetLaws(List<SiliconLaw> newLaws, EntityUid target, SoundSpecifier? cue = null)
    {
        if (!TryComp<SiliconLawProviderComponent>(target, out var component))
            return;

        if (component.Lawset == null)
            component.Lawset = new SiliconLawset();

        component.Lawset.Laws = newLaws;
        RankLaws(component.Lawset.Laws);
        NotifyLawsChanged((target,component), cue);
    }

    protected override void OnUpdaterInsert(Entity<SiliconLawUpdaterComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        // TODO: Prediction dump this
        if (!TryComp<SiliconLawProviderComponent>(args.Entity, out var provider))
            return;

        var lawset = provider.Lawset;

        var query = EntityManager.CompRegistryQueryEnumerator(ent.Comp.Components);

        while (query.MoveNext(out var update))
        {
            if (TryComp<ShowCrewIconsComponent>(update, out var crewIconComp))
            {
                crewIconComp.UncertainCrewBorder = DefaultCrewLawset != provider.Laws;
                Dirty(update, crewIconComp);
            }

            SetProviderLaws(update, lawset.Laws, false, provider.LawUploadSound);
        }
    }

    /// <summary>
    /// Updates the version on a target SiliconLawBoundComponent. This is used in the law UI as flair to show the
    /// number of updates a silicon player's laws has had
    /// </summary>
    private void UpdateLawVersion(Entity<SiliconLawBoundComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        target.Comp.Version++;
    }

    /// <summary>
    /// Given a list of laws, sets all unobfuscated laws' identifier in order from highest to lowest priority.
    /// </summary>
    /// <param name="laws">The lawset to deduce identifiers for.</param>
    public static void RankLaws(List<SiliconLaw> laws)
    {
        // Sort laws first since there can be cases where law order != list order
        laws.Sort();
        // Don't need to set any overrides if there are no corrupted laws and law order already makes sense
        // (i.e. order is non-negative and monotonically increasing by 1)
        var overrideIdentifiers = false;
        for (var i = 0; i < laws.Count; i++)
        {
            if (laws[i].Corrupted
                || laws[i].Order < 0
                || i > 0 && laws[i].Order - laws[i - 1].Order != 1)
            {
                overrideIdentifiers = true;
                break;
            }
        }
        if (!overrideIdentifiers)
        {
            return;
        }

        var orderDeduction = -1;
        for (var i = 0; i < laws.Count; i++)
        {
            if (laws[i].Corrupted)
            {
                orderDeduction += 1;
            }
            else
            {
                laws[i].LawIdentifierOverride = (i - orderDeduction).ToString();
            }
        }
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class LawsCommand : ToolshedCommand
{
    private SiliconLawSystem? _law;

    [CommandImplementation("list")]
    public IEnumerable<EntityUid> List()
    {
        var query = EntityManager.EntityQueryEnumerator<SiliconLawProviderComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            yield return uid;
        }
    }

    [CommandImplementation("get")]
    public IEnumerable<string> Get([PipedArgument] EntityUid lawbound)
    {
        _law ??= GetSys<SiliconLawSystem>();

        foreach (var law in _law.GetBoundLaws(lawbound).Laws)
        {
            yield return $"law {law.LawIdentifierOverride ?? law.Order.ToString()}: {Loc.GetString(law.LawString)}";
        }
    }
}
