using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// This handles interaction with the organs of an entity with <see cref="ExposedOrgansComponent"/>
/// </summary>
public sealed class ExposedOrgansSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExposedOrgansComponent, ComponentInit>(OnInit);

        SubscribeLocalEvent<ExposedOrgansComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInit(Entity<ExposedOrgansComponent> ent, ref ComponentInit args)
    {
        EnsureComp<BodyComponent>(ent);
    }

    // todo : replace if one already
    private void OnInteractUsing(Entity<ExposedOrgansComponent> ent, ref InteractUsingEvent ev)
    {
        if (ev.Handled)
            return;

        if (!TryComp<OrganComponent>(ev.Used, out var organ) ||
            organ.Category is not { } category ||
            !ent.Comp.ExposedCategories.Contains(category) ||
            !TryComp<BodyComponent>(ent, out var body) ||
            body.Organs is not { } organs)
            return;

        if (ent.Comp.Organs.TryGetValue(category, out var inside))
        {
            // replace the existing organ with the new one.
        }
        else
        {
            _container.Insert(ev.Used, organs);
        }


        _audio.PlayPredicted(ent.Comp.InteractionSound, ent, ev.User, AudioParams.Default);
        ev.Handled = true;
    }

    // todo : get the same ui as with slots.

    public bool TryGetOrgan(Entity<ExposedOrgansComponent> ent, ProtoId<OrganCategoryPrototype> category, [NotNullWhen(true)] out EntityUid? organ)
    {
        organ = null;
        if (ent.Comp.Organs.TryGetValue(category, out var organEnt))
            organ = organEnt;
        return organ != null;
    }
}
