using Content.Shared._Moffstation.Body.Components;
using Content.Shared._Moffstation.Body.Systems;
using Content.Shared.Body;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._Moffstation.Medical.CryoCapsule;

/// <summary>
/// This handles...
/// </summary>
public sealed class CryoCapsuleSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ConservableOrganSystem _organs = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoCapsuleComponent, ComponentInit>(OnComponentInit);

        SubscribeLocalEvent<CryoCapsuleComponent, OrganStatusQueryEvent>(OnOrganStatusQuery);
        SubscribeLocalEvent<CryoCapsuleComponent, ReviveBrainEvent>(OnReviveBrain);

        SubscribeLocalEvent<CryoCapsuleComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnComponentInit(Entity<CryoCapsuleComponent> ent, ref ComponentInit args)
    {
        EnsureComp<BodyComponent>(ent);

        // todo : make a tally of the organs already present and put them in the Organs field.
    }

    private void OnOrganStatusQuery(Entity<CryoCapsuleComponent> ent, ref OrganStatusQueryEvent args)
    {
        for (int i = 0; i < args.OrganEntries.Count; i++)
        {
            var entry = args.OrganEntries[i];

            if (entry.Category is not { } category ||
                ! ent.Comp.CanContain.Contains(category) ||
                ! ent.Comp.Organs.TryGetValue(category, out var organ))
                continue;

            if (TryComp<ReusableOrganComponent>(organ, out var consComp))
                entry = _organs.GenerateEntry((organ, consComp));
            else
                entry.Status = OrganEntry.OrganStatus.Healthy; // todo : probably unusable or something ?
            args.OrganEntries[i] = entry;
        }
    }

    private void OnReviveBrain(Entity<CryoCapsuleComponent> ent, ref ReviveBrainEvent args)
    {
        // todo
    }

    private void OnInteractUsing(Entity<CryoCapsuleComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp<OrganComponent>(args.Used, out var organComp) ||
            organComp.Category is not { } category ||
            !ent.Comp.CanContain.Contains(category))
            return;

        if (!TryComp<BodyComponent>(ent, out var bodyComp) ||
            bodyComp.Organs is not { } organs)
            return;

        if (ent.Comp.Organs.TryGetValue(category, out var present))
        {
            // todo : swap with the existing organ.
        }
        else
        {
            _container.Insert(args.Used, organs);
        }

        _audio.PlayPredicted(ent.Comp.InteractSound, ent, args.User);
        ent.Comp.Organs[category] = args.Used; // todo : it seem that it introduce a delay in the insert action.
        args.Handled = true;
    }

    #region public api

    public bool CanBeRevived(Entity<CryoCapsuleComponent> ent, out string? reason)
    {
        reason = null;

        foreach (var category in ent.Comp.CriticalOrgans)
        {
            if (ent.Comp.Organs.TryGetValue(category, out _))
                continue;

            reason = Loc.GetString("cryocapsule-unrevivable-critical-organs");
            return false;
        }


        return true;
    }

    #endregion
}
