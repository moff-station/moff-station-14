using Content.Shared._Moffstation.Body.Components;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Body.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class ConservableOrganSystem : EntitySystem
{
    private IPrototypeManager _proto = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        _proto = IoCManager.Resolve<IPrototypeManager>();
    }

    public OrganEntry GenerateEntry(Entity<ConservableOrganComponent> ent)
    {
        var specieName = _proto.TryIndex(ent.Comp.Specie, out var specie)
            ? specie.Name
            : "unknown";
        if (TryComp<OrganComponent>(ent, out var organ))
            return new OrganEntry(specieName, organ.Category, OrganEntry.OrganStatus.Healthy);

        return new OrganEntry(specieName, null, OrganEntry.OrganStatus.Healthy);

    }
}
