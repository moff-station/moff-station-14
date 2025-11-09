using Content.Shared._Moffstation.Cards;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Client._Moffstation.Cards;

public sealed class DeckVisualSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeckComponent, ComponentInit>(OnDeckInit);
        SubscribeLocalEvent<DeckComponent, EntInsertedIntoContainerMessage>(OnDeckContainerChanged);
        SubscribeLocalEvent<DeckComponent, EntRemovedFromContainerMessage>(OnDeckContainerChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Redraw every frame; simple and guarantees flip is reflected.
        var query = EntityQueryEnumerator<DeckComponent>();
        while (query.MoveNext(out var uid, out var deck))
        {
            UpdateDeckSprite(uid, deck);
        }
    }

    private void OnDeckInit(EntityUid uid, DeckComponent deck, ComponentInit args)
    {
        UpdateDeckSprite(uid, deck);
    }

    private void OnDeckContainerChanged(EntityUid uid, DeckComponent deck, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != deck.ContainerId)
            return;

        UpdateDeckSprite(uid, deck);
    }

    private void OnDeckContainerChanged(EntityUid uid, DeckComponent deck, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != deck.ContainerId)
            return;

        UpdateDeckSprite(uid, deck);
    }

    private void UpdateDeckSprite(EntityUid deckUid, DeckComponent deck)
    {
        if (!EntityManager.TryGetComponent(deckUid, out SpriteComponent? sprite))
            return;

        // Ensure we have a layer 0.
        var hasLayer0 = false;
        foreach (var _ in sprite.AllLayers)
        {
            hasLayer0 = true;
            break;
        }
        if (!hasLayer0)
            return;

        if (!_containers.TryGetContainer(deckUid, deck.ContainerId, out var cont) ||
            cont.ContainedEntities.Count == 0)
        {
            sprite.LayerSetSprite(0,
                new SpriteSpecifier.Rsi(
                    new ResPath("_Moffstation/Objects/Fun/Cards/card_special.rsi"),
                    "blank"));
            return;
        }

        var list = cont.ContainedEntities;
        var idx = list.Count - 1; // top is always last
        var top = list[idx];

        if (!EntityManager.TryGetComponent(top, out CardComponent? card))
        {
            sprite.LayerSetSprite(0,
                new SpriteSpecifier.Rsi(
                    new ResPath("_Moffstation/Objects/Fun/Cards/card_special.rsi"),
                    "blank"));
            return;
        }

        if (!deck.IsFaceUp)
        {
            // Face-down: show back of top.
            var (rsi, state) = GetBackSprite(card);
            sprite.LayerSetSprite(0, new SpriteSpecifier.Rsi(new ResPath(rsi), state));
        }
        else
        {
            // Face-up: show front of top.
            var (rsi, state) = GetFrontSprite(card);
            sprite.LayerSetSprite(0, new SpriteSpecifier.Rsi(new ResPath(rsi), state));
        }
    }

    private (string rsi, string state) GetBackSprite(CardComponent card)
    {
        if (!string.IsNullOrEmpty(card.BackRsi) && !string.IsNullOrEmpty(card.BackState))
            return (card.BackRsi!, card.BackState!);

        return ("_Moffstation/Objects/Fun/Cards/card_backs.rsi", "red");
    }

    private (string rsi, string state) GetFrontSprite(CardComponent card)
    {
        if (!string.IsNullOrEmpty(card.FrontRsi) && !string.IsNullOrEmpty(card.FrontState))
            return (card.FrontRsi!, card.FrontState!);

        var suit = card.Suit.ToLowerInvariant();
        var value = card.Value.ToLowerInvariant();

        string baseRsi;
        string state;

        switch (suit)
        {
            case "clubs":
                baseRsi = "_Moffstation/Objects/Fun/Cards/clubs.rsi";
                state = NormalizeValue(value);
                break;
            case "hearts":
                baseRsi = "_Moffstation/Objects/Fun/Cards/hearts.rsi";
                state = NormalizeValue(value);
                break;
            case "spades":
                baseRsi = "_Moffstation/Objects/Fun/Cards/spades.rsi";
                state = NormalizeValue(value);
                break;
            case "diamonds":
                baseRsi = "_Moffstation/Objects/Fun/Cards/diamonds.rsi";
                state = NormalizeValue(value);
                break;
            default:
                baseRsi = "_Moffstation/Objects/Fun/Cards/card_special.rsi";
                state = "blank";
                break;
        }

        return (baseRsi, state);
    }

    private string NormalizeValue(string v)
    {
        switch (v)
        {
            case "a":
            case "ace": return "ace";
            case "2":
            case "two": return "two";
            case "3":
            case "three": return "three";
            case "4":
            case "four": return "four";
            case "5":
            case "five": return "five";
            case "6":
            case "six": return "six";
            case "7":
            case "seven": return "seven";
            case "8":
            case "eight": return "eight";
            case "9":
            case "nine": return "nine";
            case "10":
            case "ten": return "ten";
            case "j":
            case "jack": return "jack";
            case "q":
            case "queen": return "queen";
            case "k":
            case "king": return "king";
            default: return "blank";
        }
    }
}
