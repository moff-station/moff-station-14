using Content.Shared._Harmony.BloodBrothers.Components;
using Content.Shared._Moffstation.BloodBrothers.Components;
using Content.Shared.Actions;
using Content.Shared.Antag;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Shared._Harmony.BloodBrothers.EntitySystems;

public abstract partial class BloodBrotherSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InitialBloodBrotherComponent, MapInitEvent>(OnInitialBloodBrotherMapInit);
        SubscribeLocalEvent<InitialBloodBrotherComponent, ComponentShutdown>(OnInitialBloodBrotherShutdown);
        SubscribeLocalEvent<BloodBrotherComponent, ComponentGetStateAttemptEvent>(OnBloodBrotherAttemptGetState);
    }

    private void OnInitialBloodBrotherMapInit(Entity<InitialBloodBrotherComponent> entity, ref MapInitEvent args)
    {
        _actionsSystem.AddAction(entity, ref entity.Comp.ConvertActionEntity, entity.Comp.ConvertAction);
        Dirty(entity);
        DirtyConvertibleMarkers();
    }

    [SubscribeLocalEvent]
    private void OnInitialBloodBrotherPlayerAttached(Entity<InitialBloodBrotherComponent> entity, ref PlayerAttachedEvent args)
    {
        DirtyConvertibleMarkers();
    }

    [SubscribeLocalEvent]
    private void OnConvertibleGetStateAttempt(
        Entity<BloodBrotherConvertibleComponent> entity,
        ref ComponentGetStateAttemptEvent args)
    {
        if (args.Player?.AttachedEntity is { } viewer)
            args.Cancelled = !HasComp<InitialBloodBrotherComponent>(viewer);
    }

    private void OnInitialBloodBrotherShutdown(Entity<InitialBloodBrotherComponent> entity, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(entity.Comp.ConvertActionEntity);
    }

    private void OnBloodBrotherAttemptGetState(
        Entity<BloodBrotherComponent> entity,
        ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player);
    }

    private bool CanGetState(ICommonSession? player)
    {
        //Apparently this can be null in replays so I am just returning true.
        if (player?.AttachedEntity is not {} uid)
            return true;

        return HasComp<BloodBrotherComponent>(uid) || HasComp<ShowAntagIconsComponent>(uid);
    }

    public void SetConvertible(EntityUid target, bool convertible)
    {
        if (convertible)
            EnsureComp<BloodBrotherConvertibleComponent>(target);
        else
            RemComp<BloodBrotherConvertibleComponent>(target);
    }

    public void ClearConvertible()
    {
        foreach (var ent in EntityQueryEnumerator<BloodBrotherConvertibleComponent>())
        {
            RemCompDeferred(ent.Owner, ent.Comp);
        }
    }

    // Markers added while a viewer couldn't see them are never sent unless dirtied again
    private void DirtyConvertibleMarkers()
    {
        foreach (var ent in EntityQueryEnumerator<BloodBrotherConvertibleComponent>())
        {
            Dirty(ent.Owner, ent.Comp);
        }
    }
}
