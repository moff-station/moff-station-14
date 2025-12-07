using System.Linq;
using Content.Shared._Moffstation.Cards.Components;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Cards.Systems;

/// This system implements behaviors for playing cards, including decks, hands, and cards themselves.
/// <seealso cref="PlayingCardComponent"/>
/// <seealso cref="PlayingCardDeckComponent"/>
/// <seealso cref="PlayingCardHandComponent"/>
// This part just declares dependencies and has basic shared functions.
public sealed partial class PlayingCardsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        InitCard();
        InitDeck();
        InitHand();
    }

    /// This function returns the complete sprite layers for the given <paramref name="card"/>, in the related
    /// <paramref name="deck"/>, assuming the given <paramref name="faceDownOverride"/>. This is used by the client
    /// visualizers to construct sprites for decks and hands based on their containing cards.
    /// If <paramref name="deck"/> is null, any unspawned cards' sprites will be ignored.
    /// If <paramref name="faceDownOverride"/> is null, the card's own facing state will be used rather than assuming one.
    public PrototypeLayerData[]? ToLayers(
        PlayingCardInDeck card,
        Entity<PlayingCardDeckComponent>? deck,
        bool? faceDownOverride = null
    )
    {
        switch (card)
        {
            // It's an existing entity, just return the entity's sprite layers.
            case PlayingCardInDeck.NetEnt(var netEntity):
                return NetEntToCardOrNull(netEntity)?.Comp.Sprite(faceDownOverride);
            case PlayingCardInDeck.Unspawned(PlayingCardDeckPrototypeElementData data):
                // If we can't get the deck's deck proto, we won't be able to make a complete card.
                if (deck is not { } d ||
                    !_proto.Resolve(d.Comp.Prototype, out var deckProto))
                {
                    return this.AssertOrLogError<PrototypeLayerData[]?>(
                        $"Cannot calculate layers for {nameof(PlayingCardDeckPrototypeElementData)} when failed to resolve deck prototype for deck={deck}",
                        null
                    );
                }

                var layers = ToLayers(data, deckProto);
                return faceDownOverride ?? data.FaceDown ? layers.reverse : layers.obverse;
            case PlayingCardInDeck.Unspawned(PlayingCardDeckPrototypeElementProtoRef protoRef):
                if (!_proto.Resolve(protoRef.Prototype, out var proto))
                    return null;

                if (!proto.Components.TryGetComponent<PlayingCardComponent>(_compFact, out var cardComp))
                {
                    return this.AssertOrLogError<PrototypeLayerData[]?>(
                        $"Cannot calculate layers for {nameof(PlayingCardDeckPrototypeElementProtoRef)}, prototype did not have {nameof(PlayingCardComponent)}: prototype={proto}",
                        null
                    );
                }

                return faceDownOverride ?? protoRef.FaceDown ? cardComp.ReverseSprite : cardComp.ObverseSprite;
            default:
                return this.AssertOrLogError<PrototypeLayerData[]?>(
                    $"Unknown variant of {nameof(PlayingCardInDeck)}: {card.GetType()}",
                    null
                );
        }
    }

    private Entity<PlayingCardComponent>? NetEntToCardOrNull(NetEntity netEnt)
    {
        var ent = GetEntity(netEnt);
        if (TryComp<PlayingCardComponent>(ent, out var card))
        {
            return new Entity<PlayingCardComponent>(ent, card);
        }
        else
        {
            this.AssertOrLogError(
                $"Net Entity ({netEnt}) is missing expected {nameof(PlayingCardComponent)} ({ToPrettyString(ent)})");
            return null;
        }
    }
}
