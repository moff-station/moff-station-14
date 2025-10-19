using Content.Shared._ES.Voting.Components;
using Content.Shared._ES.Voting.Results;
using Content.Shared.Atmos;
using Robust.Shared.Random;

namespace Content.Shared._ES.Voting;

public abstract partial class ESSharedVoteSystem
{
    private void InitializeOptions()
    {
        SubscribeLocalEvent<ESEntityPrototypeVoteComponent, ESGetVoteOptionsEvent>(OnGetVoteOptions);
        SubscribeLocalEvent<ESGasVoteComponent, ESGetVoteOptionsEvent>(OnGasGetVoteOptions);
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

    private void OnGasGetVoteOptions(Entity<ESGasVoteComponent> ent, ref ESGetVoteOptionsEvent args)
    {
        var gases = new List<Gas>(ent.Comp.Gases);
        var count = Math.Min(ent.Comp.Gases.Count, ent.Comp.Count);
        for (var i = 0; i < count; i++)
        {
            var gas = _random.PickAndTake(gases);
            var gasProto = _atmosphere.GetGas(gas);
            args.Options.Add(new ESGasVoteOption
            {
                DisplayString = Loc.GetString(gasProto.Name),
                Gas = gas,
            });
        }
    }
}
