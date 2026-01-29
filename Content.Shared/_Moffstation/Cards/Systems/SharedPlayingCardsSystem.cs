using System.Linq;
using Content.Shared._Moffstation.Cards.Components;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
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
public abstract partial class SharedPlayingCardsSystem : EntitySystem
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
    [Dependency] private readonly SharedVerbSystem _verb = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        InitCard();
        InitDeck();
        InitHand();
    }

    /// This function retrieves the <see cref="PlayingCardComponent"/> data for the given <paramref name="card"/>. Note
    /// that since <paramref name="card"/> may not be a spawned entity, the component may not be owned by an entity.
    /// Returns null in various cases if something goes wrong with resolving prototypes, net entities, etc.
    public PlayingCardComponent? GetComponent(PlayingCardInDeck card)
    {
        var ret = card switch
        {
            PlayingCardInDeckNetEnt(var netEntity) => NetEntToCardOrNull(netEntity)?.Comp,
            PlayingCardInDeckUnspawnedData data => ToComponent(data),
            PlayingCardInDeckUnspawnedRef(var entProtoId, var faceDown) =>
                _proto.Resolve(entProtoId, out var proto) &&
                proto.Components.TryGetComponent<PlayingCardComponent>(_compFact, out var cardComp)
                    ? WithFacing(cardComp, faceDown)
                    : null,
            _ => card.ThrowUnknownInheritor<PlayingCardInDeck, PlayingCardComponent?>(),
        };
        if (ret is null)
        {
            return this.AssertOrLogError<PlayingCardComponent?>(
                $"Failed to get {nameof(PlayingCardComponent)} from {card}",
                null
            );
        }

        return ret;
    }

    private Entity<PlayingCardComponent>? NetEntToCardOrNull(NetEntity netEnt)
    {
        var ent = GetEntity(netEnt);
        if (!TryComp<PlayingCardComponent>(ent, out var card))
        {
            this.AssertOrLogError(
                $"Net Entity ({netEnt}) is missing expected {nameof(PlayingCardComponent)} ({ToPrettyString(ent)})");
            return null;
        }

        return new Entity<PlayingCardComponent>(ent, card);
    }

    /// This function just sets the given <paramref name="comp"/>'s <see cref="PlayingCardComponent.FaceDown"/> and
    /// returns the component. This is useful for setting the component's value inline.
    private static PlayingCardComponent WithFacing(PlayingCardComponent comp, bool faceDown)
    {
        comp.FaceDown = faceDown;
        return comp;
    }
}
