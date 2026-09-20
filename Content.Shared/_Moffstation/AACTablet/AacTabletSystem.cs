using System.Collections.Frozen;
using System.Linq;
using Content.Shared.Chat;
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

    private readonly List<CachedPhrase> _cache = new();

    private readonly record struct CachedPhrase(
        string LocalizedText,
        AacPhrase Phrase,
        ProtoId<AacPhrasePackPrototype> Pack,
        string Tab,
        string? Group
    );

    private const string DelayId = nameof(AacTabletComponent);

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<AacTabletComponent>(
            AacTabletKey.Key,
            subs => subs.Event<AacTabletSendPhraseMessage>(OnSendPhrase)
        );

        PopulateCaches();
    }

    /// Returns all <see cref="AacPhrase"/>s which contain <paramref name="search"/> in their localized text.
    public (AacPhrase? shortestMatch, IEnumerable<(AacPhrase, IEnumerable<string>)> allMatches) SearchPhrases(
        string search,
        IEnumerable<AacPhrasePackPrototype> availablePacks
    )
    {
        if (search.Length == 0)
            return (null, []);

        var packs = availablePacks.Select(it => (ProtoId<AacPhrasePackPrototype>)it.ID).ToFrozenSet();

        var matches = (
                search.Length switch
                {
                    0 => throw new Exception("Unreachable"),
                    // Small search strings match way too much, so do exact matches
                    < 3 => _cache.Where(it => it.LocalizedText.Equals(search, StringComparison.OrdinalIgnoreCase)),
                    _ => _cache.Where(it => it.LocalizedText.Contains(search, StringComparison.OrdinalIgnoreCase)),
                }
            ).Where(it => packs.Contains(it.Pack))
            .OrderBy(it => it.LocalizedText)
            .ToList();

        if (matches.Count == 0)
            return (null, []);

        var shortest = matches.MinBy(it => it.LocalizedText.Length).Phrase;
        var allMatches = matches.Select(it => (it.Phrase, (IEnumerable<string>)(string[])[it.Tab, it.Group]))
            .OrderBy(it => it.Phrase.Phrase);

        return (shortest, allMatches);
    }

    private void PopulateCaches()
    {
        _cache.Clear();
        _cache.AddRange(
            _prototypeManager.EnumeratePrototypes<AacPhrasePackPrototype>()
                .SelectMany(pack =>
                    pack.Tabs.SelectMany(tab =>
                        tab.Groups.SelectMany(group =>
                            group.Phrases.Select(phrase =>
                                new CachedPhrase(
                                    phrase.Phrase,
                                    phrase,
                                    pack,
                                    tab.Heading,
                                    group.Heading
                                )
                            )
                        )
                    )
                )
        );
    }

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.Modified.Contains(typeof(AacPhrasePackPrototype)))
            return;

        PopulateCaches();
    }

    private void OnSendPhrase(Entity<AacTabletComponent> ent, ref AacTabletSendPhraseMessage message)
    {
        if (_useDelaySystem.IsDelayed(ent.Owner, DelayId))
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
            message.Phrase.Phrase,
            InGameICChatType.Speak,
            hideChat: false,
            nameOverride: speakerName
        );

        _useDelaySystem.SetLength(ent.Owner, ent.Comp.Cooldown, DelayId);
    }
}

[Serializable, NetSerializable]
public enum AacTabletKey : byte { Key }

[Serializable, NetSerializable]
public sealed class AacTabletSendPhraseMessage(AacPhrase phrase) : BoundUserInterfaceMessage
{
    public AacPhrase Phrase = phrase;
}
