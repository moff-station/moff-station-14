using Content.Shared._Moffstation.Physics;
using Content.Shared._Moffstation.Sight.Components;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._Moffstation.Sight.Systems;

public sealed partial class SightSensorSystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private FixtureSystem _fixtures = default!;

    [Dependency] private SharedChatSystem _chat = default!;


    // don't collide if our fixture is the one in the comp and args don't have target comp OR not good key
    [SubscribeLocalEvent]
    private void OnPreventCollide(Entity<SightSensorComponent> ent, ref PreventCollideEvent args)
    {
        _chat.DispatchGlobalAnnouncement("prevented !");
        if (args.OurFixture != _fixtures.GetFixtureOrNull(ent, ent.Comp.FixtureId))
            return;

        if (!TryComp<SightTargetComponent>(args.OtherEntity, out var target) ||
            target.Key != ent.Comp.Key)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnStartCollide(Entity<SightSensorComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        _chat.DispatchGlobalAnnouncement("collided !");

        var ev = new EnterSightlineEvent(args.OtherEntity);
        RaiseLocalEvent(ent, ref ev);
    }

    [SubscribeLocalEvent]
    private void OnStopCollide(Entity<SightSensorComponent> ent, ref EndCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        var ev = new ExitSightlineEvent(args.OtherEntity);
        RaiseLocalEvent(ent, ref ev);
    }
}
