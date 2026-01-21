using System.Linq;
using Content.Shared._Moffstation.Cards.Components;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Shared._Moffstation.Cards.Systems;

// This part handles behavior common to all PlayingCardStackComponent-derived components.
public abstract partial class SharedPlayingCardsSystem
{
    private static readonly AudioParams AudioVariation = AudioParams.Default.WithVariation(0.05f);

    private static void DirtyVisuals<TStack, TArgs>(Entity<TStack> entity, ref TArgs args)
        where TStack : PlayingCardStackComponent
    {
        entity.Comp.DirtyVisuals = true;
    }

    private void OnStartup<TStack>(Entity<TStack> entity, ref ComponentStartup args)
        where TStack : PlayingCardStackComponent
    {
        entity.Comp.Container = _container.EnsureContainer<Container>(entity, entity.Comp.ContainerId);
    }

    private void OnExamined<TStack>(Entity<TStack> entity, ref ExaminedEvent args)
        where TStack : PlayingCardStackComponent
    {
        args.PushText(Loc.GetString("card-stack-examine", ("count", entity.Comp.NumCards)));
    }

    private void OnGetAlternativeVerbsCommon<TStack>(Entity<TStack> entity, ref GetVerbsEvent<AlternativeVerb> args)
        where TStack : PlayingCardStackComponent
    {
        if (args.Using == args.Target ||
            args.Using is not { } usedEnt)
            return;

        var user = args.User;

        if (TryComp<PlayingCardDeckComponent>(args.Using, out var usedDeck))
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString(entity.Comp.JoinText),
                Icon = entity.Comp.JoinIcon,
                Priority = 8,
                Act = () => Transfer<PlayingCardDeckComponent, TStack>((usedEnt, usedDeck), entity, .., user),
            });
        }

        if (TryComp<PlayingCardHandComponent>(args.Using, out var usedHand))
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString(entity.Comp.JoinText),
                Icon = entity.Comp.JoinIcon,
                Priority = 8,
                Act = () => Transfer<PlayingCardHandComponent, TStack>((usedEnt, usedHand), entity, .., user),
            });
        }

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => FlipAll(entity, false, user),
            Text = Loc.GetString(entity.Comp.OrganizeUpText),
            Icon = entity.Comp.FlipCardsIcon,
            Priority = 1,
        });
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => FlipAll(entity, true, user),
            Text = Loc.GetString(entity.Comp.OrganizeDownText),
            Icon = entity.Comp.FlipCardsIcon,
            Priority = 2,
        });
    }

    /// Flips all cards in the given stack entity, handling audio, dirtying visuals, etc.
    private void FlipAll<T>(Entity<T> entity, bool faceDown, EntityUid? user)
        where T : PlayingCardStackComponent
    {
        var didAnyFlip = entity.Comp switch
        {
            PlayingCardDeckComponent deck => deck.Cards.Aggregate(
                false,
                (current, card) => current | FlipCardInDeck(card)
            ),
            PlayingCardHandComponent hand => hand.Cards.Aggregate(false, (current, card) => current | FlipNetEnt(card)),
            _ => entity.Comp.ThrowUnknownInheritor<PlayingCardStackComponent, bool>(),
        };

        if (didAnyFlip)
        {
            entity.Comp.DirtyVisuals = true;
        }

        _audio.PlayPredicted(entity.Comp.ShuffleSound, entity, user, AudioVariation);

        bool FlipCardInDeck(PlayingCardInDeck card) => card switch
        {
            PlayingCardInDeckNetEnt(var cardNetEnt) => FlipNetEnt(cardNetEnt),
            PlayingCardInDeckUnspawnedData(var data, _, _) => SetOrInvert(ref data.FaceDown, faceDown),
            PlayingCardInDeckUnspawnedRef(_, var fd) => SetOrInvert(ref fd, faceDown),
            _ => card.ThrowUnknownInheritor<PlayingCardInDeck, bool>(),
        };

        bool FlipNetEnt(NetEntity cardNetEnt) => GetEntity(cardNetEnt) is var cardEnt &&
                                                 TryComp<PlayingCardComponent>(cardEnt, out var cardComp) &&
                                                 SetFacingOrFlip((cardEnt, cardComp), faceDown);
    }

    private void OnInteractUsing<TStack>(Entity<TStack> entity, ref InteractUsingEvent args)
        where TStack : PlayingCardStackComponent
    {
        if (TryComp<PlayingCardComponent>(args.Used, out var usedCard))
        {
            Entity<PlayingCardComponent> card = (args.Used, usedCard);
            Add(entity, [card], Transform(args.User).Coordinates, args.User);
            args.Handled = true;
            return;
        }

        if (TryComp<PlayingCardDeckComponent>(args.Used, out var usedDeck))
        {
            Transfer<TStack, PlayingCardDeckComponent>(
                entity,
                (args.Used, usedDeck),
                ^1..,
                args.User
            );
            args.Handled = true;
            return;
        }

        if (TryComp<PlayingCardHandComponent>(args.Used, out var usedHand))
        {
            Transfer<TStack, PlayingCardHandComponent>(
                entity,
                (args.Used, usedHand),
                ^1..,
                args.User
            );
            args.Handled = true;
            return;
        }
    }

    /// Updates the visuals of any decks or hands with dirty visuals.
    public override void Update(float frameTime)
    {
        Update<PlayingCardDeckComponent>(deck => deck.Cards);
        Update<PlayingCardHandComponent>(hand => hand.Cards.Select(it => new PlayingCardInDeckNetEnt(it)));
    }

    private void Update<TStack>(Func<TStack, IEnumerable<PlayingCardInDeck>> cardAccessor)
        where TStack : PlayingCardStackComponent
    {
        // Iterate through all stacks of the given type.
        var stacks = EntityQueryEnumerator<TStack>();
        while (stacks.MoveNext(out var ent, out var comp))
        {
            // If they're not dirty, or they're gonna be deleted, skip them.
            if (!comp.DirtyVisuals ||
                Deleted(ent))
                continue;

            // Set the visible cards based on the cards currently in the stack.
            _appearance.SetData(
                ent,
                PlayingCardStackVisuals.Cards,
                cardAccessor(comp).Take(^Math.Min(comp.VisualLimit, comp.NumCards)..).ToList()
            );

            // No longer dirty.
            comp.DirtyVisuals = false;
        }
    }
}
