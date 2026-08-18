using Content.Shared._Moffstation.Pinpointer;
using Content.Shared._Moffstation.Radio;
using Robust.Shared.Map;

namespace Content.Shared._Moffstation.Warp;

/// <summary>
/// This handles the ability to teleport to a certain location <see cref="WarpComponent"/>
/// </summary>
public abstract class SharedWarpSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioWarpComponent, RadioWarpEnabledQuery>(OnRadioWarpEnabledQuery);
        SubscribeLocalEvent<NavMapWarpComponent, NavMapWarpEnabledQuery>(OnNavMapWarpEnabledQuery);
        SubscribeLocalEvent<NavMapWarpComponent, NavMapWarpRequest>(OnNavMapWarpRequest);
    }

    private static void OnRadioWarpEnabledQuery(Entity<RadioWarpComponent> ent, ref RadioWarpEnabledQuery args)
    {
        args.Enabled = true;
    }

    private static void OnNavMapWarpEnabledQuery(Entity<NavMapWarpComponent> ent, ref NavMapWarpEnabledQuery args)
    {
        args.Enabled = true;
    }

    private void OnNavMapWarpRequest(Entity<NavMapWarpComponent> ent, ref NavMapWarpRequest args)
    {
        RequestWarpToLocation(args.Coordinates);
    }

    public void RequestWarpToEntity(NetEntity target)
    {
        RaiseNetworkEvent(new WarpRequestEvent(new EntityWarpTarget(target)));
    }

    public void RequestWarpToLocation(NetCoordinates target)
    {
        RaiseNetworkEvent(new WarpRequestEvent(new CoordinatesWarpTarget(target)));
    }

}
