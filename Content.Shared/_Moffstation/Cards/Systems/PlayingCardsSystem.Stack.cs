using System.Linq;
using Content.Shared._Moffstation.Cards.Components;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Random.Helpers;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Utility;

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
        args.PushText(Loc.GetString(entity.Comp.ExamineText, ("count", entity.Comp.NumCards)));
    }

    private void OnGetUtilityVerbsStack<TStack>(Entity<TStack> entity, ref GetVerbsEvent<UtilityVerb> args)
        where TStack : PlayingCardStackComponent
    {
        var user = args.User;
        var target = args.Target;
        if (TryComp<PlayingCardDeckComponent>(target, out var targetDeck))
        {
            // Draw card to the held stack
            args.Verbs.Add(new UtilityVerb
            {
                Text = Loc.GetString(entity.Comp.DrawToText),
                Icon = entity.Comp.DrawIcon,
                Priority = 99,
                Act = () => Transfer<PlayingCardDeckComponent, TStack>((target, targetDeck), entity, ^1.., user),
            });

            if (entity.Comp is not PlayingCardHandComponent) // Don't try to join decks into stacks. I think the engine doesn't like 10s of predicted entities at once.
            {
                // Join target deck to held stack
                args.Verbs.Add(new UtilityVerb
                {
                    Text = Loc.GetString(entity.Comp.JoinText),
                    Icon = entity.Comp.JoinIcon,
                    Act = () => Transfer<PlayingCardDeckComponent, TStack>((target, targetDeck), entity, .., user),
                });
            }
        }

        if (TryComp<PlayingCardHandComponent>(target, out var targetHand))
        {
            // Draw card to the held stack
            args.Verbs.Add(new UtilityVerb
            {
                Text = Loc.GetString(entity.Comp.DrawToText),
                Icon = entity.Comp.DrawIcon,
                Priority = 99,
                Act = () => Transfer<PlayingCardHandComponent, TStack>((target, targetHand), entity, ^1.., user),
            });

            // Join target hand to held stack
            args.Verbs.Add(new UtilityVerb
            {
                Text = Loc.GetString(entity.Comp.JoinText),
                Icon = entity.Comp.JoinIcon,
                Act = () => Transfer<PlayingCardHandComponent, TStack>((target, targetHand), entity, .., user),
            });
        }
    }

    private void OnGetAlternativeVerbsStack<TStack>(Entity<TStack> entity, ref GetVerbsEvent<AlternativeVerb> args)
        where TStack : PlayingCardStackComponent
    {
        var user = args.User;

        // Flip all cards
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => FlipAll(entity, null, user),
            Text = Loc.GetString(entity.Comp.FlipText),
            Icon = entity.Comp.FlipCardsIcon,
        });

        // Flip all cards up
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => FlipAll(entity, false, user),
            Text = Loc.GetString(entity.Comp.OrganizeUpText),
            Icon = entity.Comp.FlipCardsIcon,
        });

        // Flip all cards down
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => FlipAll(entity, true, user),
            Text = Loc.GetString(entity.Comp.OrganizeDownText),
            Icon = entity.Comp.FlipCardsIcon,
        });

        // Shuffle
        if (entity.Comp.NumCards > 1)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Act = () => Shuffle(entity, user),
                Text = Loc.GetString(entity.Comp.ShuffleText),
                Icon = entity.Comp.ShuffleIcon,
            });
        }
    }

    /// Flips all cards in the given stack entity, handling audio, dirtying visuals, etc.
    private void FlipAll<T>(Entity<T> entity, bool? faceDown, EntityUid user)
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

        var targetName = MetaData(entity).EntityName;
        _popup.PopupPredicted(
            Loc.GetString(faceDown switch
                {
                    true => entity.Comp.OrganizeDownPopup,
                    false => entity.Comp.OrganizeUpSuccessPopup,
                    null => entity.Comp.FlipPopup,
                },
                ("target", targetName)
            ),
            Loc.GetString(
                faceDown switch
                {
                    true => entity.Comp.OrganizeDownSuccessPopupOther,
                    false => entity.Comp.OrganizeUpSuccessPopupOther,
                    null => entity.Comp.FlipPopupOther,
                },
                ("target", targetName),
                ("user", Identity.Name(user, EntityManager))
            ),
            entity,
            user
        );
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
        if (args.Handled || args.Used == args.Target)
            return;

        if (TryComp<PlayingCardComponent>(args.Used, out var usedCard))
        {
            Add(entity, [(args.Used, usedCard)], Transform(args.User).Coordinates, args.User);
            args.Handled = true;
            return;
        }

        if (TryComp<PlayingCardDeckComponent>(args.Used, out var usedDeck))
        {
            Transfer<TStack, PlayingCardDeckComponent>(entity, (args.Used, usedDeck), ^1.., args.User);
            args.Handled = true;
            return;
        }

        if (TryComp<PlayingCardHandComponent>(args.Used, out var usedHand))
        {
            Transfer<TStack, PlayingCardHandComponent>(entity, (args.Used, usedHand), ^1.., args.User);
            args.Handled = true;
            return;
        }
    }

    /// Shuffles all cards in the given stack entity, handling audio, dirtying visuals, etc.
    private void Shuffle<T>(Entity<T> entity, EntityUid user) where T : PlayingCardStackComponent
    {
        var rand = new System.Random(
            SharedRandomExtensions.HashCodeCombine((int)_gameTiming.CurTick.Value, entity.Owner.Id));
        switch (entity.Comp)
        {
            case PlayingCardDeckComponent deck:
                rand.Shuffle(deck.Cards);
                break;
            case PlayingCardHandComponent hand:
                rand.Shuffle(hand.Cards);
                break;
            default:
                entity.Comp.ThrowUnknownInheritor<PlayingCardStackComponent>();
                break;
        }

        Dirty(entity);
        entity.Comp.DirtyVisuals = true;

        var targetName = MetaData(entity).EntityName;
        _popup.PopupPredicted(
            Loc.GetString(entity.Comp.ShufflePopup, ("target", targetName)),
            Loc.GetString(
                entity.Comp.ShufflePopupOther,
                ("target", targetName),
                ("user", Identity.Name(user, EntityManager))
            ),
            entity,
            user
        );
        _audio.PlayPredicted(entity.Comp.ShuffleSound, entity, user, AudioVariation);
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
