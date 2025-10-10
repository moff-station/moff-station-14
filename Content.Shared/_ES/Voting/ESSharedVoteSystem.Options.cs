using Content.Shared._ES.Voting.Components;

namespace Content.Shared._ES.Voting;

public abstract partial class ESSharedVoteSystem
{
    private void InitializeOptions()
    {
        SubscribeLocalEvent<ESEntityPrototypeVoteComponent, ESGetVoteOptionsEvent>(OnGetVoteOptions);
    }

    private void OnGetVoteOptions(Entity<ESEntityPrototypeVoteComponent> ent, ref ESGetVoteOptionsEvent args)
    {
        var entities = _entityTable.GetSpawns(ent.Comp.Options);
        foreach (var entProtoId in entities)
        {
            var entProto = _prototype.Index(entProtoId);
            args.Options.Add(new ESEntityPrototypeVoteOption
            {
                DisplayString = entProto.Name,
                Entity = entProto,
            });
        }
    }
}
