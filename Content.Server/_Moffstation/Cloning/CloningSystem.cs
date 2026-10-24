using Content.Shared.Access.Components;
using Content.Shared.Cloning.Events;

// ReSharper disable once CheckNamespace // Moff addition to upstream class
namespace Content.Server.Cloning;

public sealed partial class CloningSystem
{
    [Dependency] private EntityQuery<IdCardComponent> _idCardQuery;

    [SubscribeLocalEvent]
    private void OnCloneItemIdCard(Entity<IdCardComponent> ent, ref CloningItemEvent args)
    {
        if (!_idCardQuery.TryComp(args.CloneUid, out var cloneComp))
            return;

        _card.TryChangeFullName(args.CloneUid, ent.Comp.FullName, cloneComp);
        _card.TryChangeJobTitle(args.CloneUid, ent.Comp.LocalizedJobTitle, cloneComp);
    }
}
