using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Physics.Components;

namespace Content.Shared._Moffstation.Cards;

public abstract class SharedDeckSystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem Containers = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    protected ISawmill Sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        Sawmill = _logManager.GetSawmill("cards");
    }

    protected bool TryGetDeckContainer(EntityUid deck, DeckComponent deckComp, out BaseContainer cont)
    {
        if (!Containers.TryGetContainer(deck, deckComp.ContainerId, out var found))
        {
            Sawmill.Warning($"[cards] Deck {ToPrettyString(deck)} missing container '{deckComp.ContainerId}'");
            cont = default!;
            return false;
        }

        cont = found!;
        return true;
    }

    // Normalize: ensure "top" is the last element.
    protected void NormalizeTopAsLast(EntityUid deckUid, DeckComponent deck)
    {
        if (!TryGetDeckContainer(deckUid, deck, out var cont))
            return;

        var cards = new List<EntityUid>();
        foreach (var e in cont.ContainedEntities)
            cards.Add(e);

        if (cards.Count <= 1)
            return;

        // If YAML already lists bottom->top (1..6), last is top; nothing to do.
        // If it was reversed, flip it once here.
        // We detect "obvious" reversed decks: if first card has higher value than last, reverse.
        if (!EntityManager.TryGetComponent(cards[0], out CardComponent? first) ||
            !EntityManager.TryGetComponent(cards[cards.Count - 1], out CardComponent? last))
            return;

        // Simple heuristic; adjust if needed.
        var looksReversed = first.ValueOrder > last.ValueOrder;

        if (!looksReversed)
            return;

        // Clear and reinsert reversed so last becomes intended top.
        foreach (var e in cards)
        {
            var rem = new Entity<TransformComponent?, MetaDataComponent?>(e, null, null);
            Containers.Remove(rem, cont, reparent: false, force: true);
        }

        for (var i = cards.Count - 1; i >= 0; i--)
        {
            var ins = new Entity<TransformComponent?, MetaDataComponent?, PhysicsComponent?>(cards[i], null, null, null);
            Containers.Insert(ins, cont);
        }
    }

    public bool TryPutOnTop(EntityUid deckUid, DeckComponent deck, EntityUid cardUid, CardComponent cardComp)
    {
        if (!TryGetDeckContainer(deckUid, deck, out var cont))
            return false;

        if (Containers.TryGetContainingContainer(cardUid, out var existing))
        {
            var rem = new Entity<TransformComponent?, MetaDataComponent?>(cardUid, null, null);
            Containers.Remove(rem, existing, reparent: false, force: true);
        }

        // Match face to deck orientation at time of insert.
        cardComp.IsFaceUp = deck.IsFaceUp;
        Dirty(cardUid, cardComp);

        // Always append to end: new top.
        var toInsert = new Entity<TransformComponent?, MetaDataComponent?, PhysicsComponent?>(cardUid, null, null, null);
        Containers.Insert(toInsert, cont);

        return true;
    }

    public bool TryDrawTop(EntityUid deckUid, DeckComponent deck, out EntityUid cardUid, out CardComponent? cardComp)
    {
        cardUid = EntityUid.Invalid;
        cardComp = null;

        if (!TryGetDeckContainer(deckUid, deck, out var cont))
            return false;

        var list = cont.ContainedEntities;
        if (list.Count == 0)
            return false;

        var idx = list.Count - 1;
        var top = list[idx];

        if (!EntityManager.TryGetComponent(top, out CardComponent? c))
        {
            Sawmill.Warning($"[cards] Deck {ToPrettyString(deckUid)} had non-card entity {ToPrettyString(top)}, removing.");
            var bad = new Entity<TransformComponent?, MetaDataComponent?>(top, null, null);
            Containers.Remove(bad, cont, reparent: false, force: true);
            return false;
        }

        var rem2 = new Entity<TransformComponent?, MetaDataComponent?>(top, null, null);
        Containers.Remove(rem2, cont, reparent: false, force: true);

        c.IsFaceUp = deck.IsFaceUp;
        Dirty(top, c);

        cardUid = top;
        cardComp = c;
        return true;
    }

    public void FlipDeck(EntityUid deckUid, DeckComponent deck)
    {
        deck.IsFaceUp = !deck.IsFaceUp;
        Dirty(deckUid, deck);

        // Reverse order: [1,2,3,4,5,6] -> [6,5,4,3,2,1]
        if (!TryGetDeckContainer(deckUid, deck, out var cont))
            return;

        var cards = new List<EntityUid>();
        foreach (var e in cont.ContainedEntities)
            cards.Add(e);

        if (cards.Count <= 1)
            return;

        foreach (var e in cards)
        {
            var rem = new Entity<TransformComponent?, MetaDataComponent?>(e, null, null);
            Containers.Remove(rem, cont, reparent: false, force: true);
        }

        for (var i = cards.Count - 1; i >= 0; i--)
        {
            var ins = new Entity<TransformComponent?, MetaDataComponent?, PhysicsComponent?>(cards[i], null, null, null);
            Containers.Insert(ins, cont);
        }
    }
}
