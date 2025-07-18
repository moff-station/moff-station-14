using Content.Shared._Moffstation.Pirate.Components;
using Content.Shared.Antag;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Shared._Moffstation.Pirate.Systems;

public abstract class SharedPirateSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PirateComponent, ComponentGetStateAttemptEvent>(OnPirateAttemptGetState);
    }

    private void OnPirateAttemptGetState(
        Entity<PirateComponent> entity,
        ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player);
    }

    private bool CanGetState(ICommonSession? player)
    {
        //Apparently this can be null in replays so I am just returning true.
        if (player?.AttachedEntity is not {} uid)
            return true;

        return HasComp<PirateComponent>(uid);
    }
}
