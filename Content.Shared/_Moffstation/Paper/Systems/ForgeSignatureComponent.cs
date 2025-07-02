using Content.Shared._Moffstation.Paper.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;

namespace Content.Shared._Moffstation.Paper.Systems;

public sealed class ForgeSignatureSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForgeSignatureComponent, ForgedSignatureChangedMessage>(ForgedSignatureLabelChanged);
    }

    private void ForgedSignatureLabelChanged(Entity<ForgeSignatureComponent> ent, ref ForgedSignatureChangedMessage args)
    {
        var signature = args.Signature.Trim();
        ent.Comp.Signature = signature[..Math.Min(ent.Comp.MaxSignatureLength, signature.Length)];
        // UpdateUI((uid, handLabeler));
        Dirty(ent.Owner, ent.Comp);

        // Log label change
        _adminLogger.Add(
            LogType.Action,
            LogImpact.Low,
     $"{ToPrettyString(args.Actor):user} set {ToPrettyString(ent.Owner):labeler} to apply label \"{ent.Comp.Signature}\"");
    }
}
