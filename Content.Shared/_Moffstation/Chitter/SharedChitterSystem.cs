using Content.Shared.Examine;
using Content.Shared.NameIdentifier;

namespace Content.Shared._Moffstation.Chitter;

public abstract partial class SharedChitterSystem : EntitySystem
{
    public override void Initialize()
    {
    }

    protected void OnExamined(Entity<ChitterAccountComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.AccountId != 0)
            args.PushMarkup(Loc.GetString("chitter-account-examine", ("number", $"#{ent.Comp.AccountId:D4}")));
    }

    public void SetAccountId(Entity<ChitterAccountComponent> ent, uint id)
    {
        ent.Comp.AccountId = id;
        Dirty(ent);
    }

    public void SetProfilePictureId(Entity<ChitterAccountComponent> ent, string id)
    {
        ent.Comp.ProfilePictureId = id;
        Dirty(ent);
    }
}
