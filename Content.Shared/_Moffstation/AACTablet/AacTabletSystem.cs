using System.Linq;
using Content.Shared.Chat;
using Content.Shared.DeltaV.QuickPhrase;
using Content.Shared.IdentityManagement;
using Content.Shared.Timing;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.AacTablet;

public sealed partial class AacTabletSystem : EntitySystem
{
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private UseDelaySystem _useDelaySystem = default!;

    private readonly List<AacTabletPhraseTab> _phraseTabs = new();

    private const string DelayId = nameof(AacTabletComponent);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        Subs.BuiEvents<AacTabletComponent>(AacTabletKey.Key,
            subs =>
            {
                subs.Event<AacTabletSendPhraseMessage>(OnSendPhrase);
            });

        PopulateCaches();
    }

    /// Returns all <see cref="QuickPhrasePrototype"/>s, organized by tab and group. The ordering of the tabs and groups
    /// is meaningful and should be maintained in the UI.
    public IEnumerable<AacTabletPhraseTab> GetPhrases() => _phraseTabs;

    /// Returns all <see cref="QuickPhrasePrototype"/>s which contain <paramref name="search"/> in their localized text.
    public (QuickPhrasePrototype? shortestMatch, IEnumerable<AacTabletPhraseGroup> allMatches) SearchPhrases(
        string search
    )
    {
        if (search.Length == 0)
            return (null, []);

        var matches = (
                search.Length switch
                {
                    0 => throw new Exception("Unreachable"),
                    // Small search strings match way too much, so do exact matches
                    < 3 => _prototypeManager.EnumeratePrototypes<QuickPhrasePrototype>()
                        .Where(it => it.LocalizedText.Equals(search, StringComparison.OrdinalIgnoreCase)),
                    _ => _prototypeManager.EnumeratePrototypes<QuickPhrasePrototype>()
                        .Where(it => it.LocalizedText.Contains(search, StringComparison.OrdinalIgnoreCase)),
                }
            ).OrderBy(it => it.LocalizedText)
            .ToList();
        var shortest = matches.MinBy(it => it.LocalizedText.Length);
        var allMatches = matches.GroupBy(it => it.Group)
            .Select(it => AacTabletPhraseGroup.Create(it.Key, it))
            .OrderBy(it => it.Name);

        return (shortest, allMatches);
    }

    private void PopulateCaches()
    {
        _phraseTabs.Clear();

        var tabs = _prototypeManager.EnumeratePrototypes<QuickPhrasePrototype>()
            .GroupBy(it => it.Tab)
            .Select(it => AacTabletPhraseTab.Create(it.Key, it))
            .OrderBy(it => it.Name);
        _phraseTabs.AddRange(tabs);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.Modified.Contains(typeof(QuickPhrasePrototype)))
            return;

        PopulateCaches();
    }

    private void OnSendPhrase(Entity<AacTabletComponent> ent, ref AacTabletSendPhraseMessage message)
    {
        if (_useDelaySystem.IsDelayed(ent.Owner, DelayId) ||
            !_prototypeManager.Resolve(message.Phrase, out var phrase))
            return;

        // the AAC tablet uses the name of the person who pressed the tablet button
        // for quality of life
        var senderName = Identity.Entity(message.Actor, EntityManager);
        var speakerName = Loc.GetString(
            "speech-name-relay",
            ("speaker", Name(ent)),
            ("originalName", senderName)
        );

        _chat.TrySendInGameICMessage(
            ent,
            phrase.LocalizedText,
            InGameICChatType.Speak,
            hideChat: false,
            nameOverride: speakerName
        );

        _useDelaySystem.SetLength(ent.Owner, ent.Comp.Cooldown, DelayId);
    }
}

public record struct AacTabletPhraseTab(string Name, List<AacTabletPhraseGroup> Groups)
{
    public static AacTabletPhraseTab Create(LocId id, IEnumerable<QuickPhrasePrototype> phrases) => new(
        Loc.GetString(id),
        phrases.GroupBy(it => it.Group)
            .Select(it => AacTabletPhraseGroup.Create(it.Key, it))
            .OrderBy(it => it.Name)
            .ToList()
    );
}

public record struct AacTabletPhraseGroup(string Name, List<QuickPhrasePrototype> Phrases)
{
    public static AacTabletPhraseGroup Create(LocId id, IEnumerable<QuickPhrasePrototype> phrases) => new(
        Loc.GetString(id),
        phrases.OrderBy(it => it.LocalizedText).ToList()
    );
}

[Serializable, NetSerializable]
public enum AacTabletKey : byte { Key }

[Serializable, NetSerializable]
public sealed class AacTabletSendPhraseMessage(ProtoId<QuickPhrasePrototype> phrase) : BoundUserInterfaceMessage
{
    public ProtoId<QuickPhrasePrototype> Phrase = phrase;
}
