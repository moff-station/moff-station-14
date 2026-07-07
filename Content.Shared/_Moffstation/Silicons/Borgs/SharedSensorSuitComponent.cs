using Content.Shared.Chat;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared._Moffstation.Silicons.Borgs;

public sealed partial class SharedSensorSuitComponent : EntitySystem
{
    [Dependency] private SharedSuitSensorSystem _suitSensor = default!;
    [Dependency] private SharedChatSystem _chat = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SensorContainerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<SensorContainerComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<SensorContainerComponent, GetVerbsEvent<Verb>>(OnVerb);
    }

    private void OnInserted(Entity<SensorContainerComponent> ent, ref EntInsertedIntoContainerMessage ev)
    {
        if (ev.Container.ID != ent.Comp.ContainerId)
            return;

        _chat.DispatchGlobalAnnouncement("bless you");

        ent.Comp.SensorEntity = ev.Entity;
    }

    private void OnRemoved(Entity<SensorContainerComponent> ent, ref EntRemovedFromContainerMessage ev)
    {
        if (ev.Container.ID != ent.Comp.ContainerId)
            return;

        ent.Comp.SensorEntity = null;
    }

    private void OnVerb(Entity<SensorContainerComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (ent.Comp.SensorEntity is not { } sensor ||
            !TryComp<SuitSensorComponent>(sensor, out var sensorComponent))
        {
            _chat.DispatchGlobalAnnouncement("fuck you");
            return;
        }

        var sensorEnt = (sensor, sensorComponent);
        args.Verbs.UnionWith(new[]
        {
            _suitSensor.CreateVerb(sensorEnt, args.User, SuitSensorMode.SensorOff),
            _suitSensor.CreateVerb(sensorEnt, args.User, SuitSensorMode.SensorBinary),
            _suitSensor.CreateVerb(sensorEnt, args.User, SuitSensorMode.SensorVitals),
            _suitSensor.CreateVerb(sensorEnt, args.User, SuitSensorMode.SensorCords),
        });
    }
}
