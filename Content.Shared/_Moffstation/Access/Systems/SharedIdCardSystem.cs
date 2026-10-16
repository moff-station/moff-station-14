using Content.Shared._Moffstation.Chitter;
using Content.Shared.Access.Components;

// ReSharper disable once CheckNamespace // Moffstation addition to upstream class
namespace Content.Shared.Access.Systems;

public abstract partial class SharedIdCardSystem
{
    [SubscribeLocalEvent]
    private static void On(Entity<IdCardComponent> entity, ref ChitterGetIdentityInfoEvent args)
    {
        args.Name = entity.Comp.FullName ?? ChitterAccount.Unknown.Name;
        args.Title = entity.Comp.LocalizedJobTitle ?? ChitterAccount.Unknown.JobTitle;
    }
}
