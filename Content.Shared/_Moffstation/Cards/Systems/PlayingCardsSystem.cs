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

    /// This function returns the complete sprite layers for the given <paramref name="card"/>, assuming the given
    /// <paramref name="faceDownOverride"/>. This is used by the client visualizers to construct sprites for decks and
    /// hands based on their containing cards.
    /// If <paramref name="faceDownOverride"/> is null, the card's own facing state will be used rather than assuming one.
    public PrototypeLayerData[]? ToLayers(PlayingCardInDeck card, bool? faceDownOverride = null)
    {
        var c = card switch
        {
            PlayingCardInDeck.NetEnt(var netEntity) => NetEntToCardOrNull(netEntity)?.Comp,
            PlayingCardInDeck.UnspawnedData data => ToComponent(data),
            PlayingCardInDeck.UnspawnedRef(var entProtoId, var faceDown) =>
                _proto.Resolve(entProtoId, out var proto) &&
                proto.Components.TryGetComponent<PlayingCardComponent>(_compFact, out var cardComp)
                    ? WithFacing(cardComp, faceDown)
                    : null,
            _ => card.ThrowUnknownInheritor<PlayingCardInDeck, PlayingCardComponent?>(),
        };
        if (c is null)
            return this.AssertOrLogError<PrototypeLayerData[]?>(
                $"Failed to get {nameof(PlayingCardComponent)} from {card}",
                null);

        return c.Sprite(faceDownOverride);
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

    /// This function just sets the given <paramref name="comp"/>'s <see cref="PlayingCardComponent.FaceDown"/> and
    /// returns the component. This is useful for setting the component's value inline.
    private static PlayingCardComponent WithFacing(PlayingCardComponent comp, bool faceDown)
    {
        comp.FaceDown = faceDown;
        return comp;
    }
}
