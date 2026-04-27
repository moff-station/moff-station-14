using Content.Shared.Body;
using Content.Shared.Interaction;

namespace Content.Shared._Moffstation.Medical.CryoCapsule;

/// <summary>
/// This handles...
/// </summary>
public sealed class CryoCapsuleSystem : EntitySystem
{
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

        // todo : make a tally of the organs already present.
    }

    private void OnOrganStatusQuery(Entity<CryoCapsuleComponent> ent, ref OrganStatusQueryEvent args)
    {
        // todo : of course we will need more details, we'll add a component to the organs
        //        once organ damage is implemented.
        foreach (var category in args.OrgansStatus.Keys)
        {
            if (!ent.Comp.CanContain.Contains(category) || !ent.Comp.Organs.ContainsKey(category))
                args.OrgansStatus[category] = OrganStatusQueryEvent.OrganStatus.Absent;
            else
                args.OrgansStatus[category] = OrganStatusQueryEvent.OrganStatus.Healthy;
        }
    }

    private void OnReviveBrain(Entity<CryoCapsuleComponent> ent, ref ReviveBrainEvent args)
    {

    }

    private void OnInteractUsing(Entity<CryoCapsuleComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp<OrganComponent>(args.Used, out var organComp))
            return;

        
    }

}
