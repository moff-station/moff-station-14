using System.Linq;
using Content.Server.Medical.SuitSensors;
using Content.Server.Silicons.Borgs;
using Content.Shared._Moffstation.Robotics.RoboticsConsole;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Robotics;
using Robust.Server.GameObjects;

namespace Content.Server._Moffstation.Robotics.RoboticsConsole;

public sealed partial class MoffRoboticsConsoleSystem : SharedMoffRoboticsConsoleSystem
{
    [Dependency] private SuitSensorSystem _sensors = default!;
    [Dependency] private BorgSystem _borgs = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        Subs.BuiEvents<MoffRoboticsConsoleComponent>(RoboticsConsoleUiKey.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnOpened);
                subs.Event<RoboticsConsoleDisableMessage>(OnDisable);
                subs.Event<RoboticsConsoleDestroyMessage>(OnDestroy);
            });
    }

    [SubscribeLocalEvent]
    private void OnPacketReceived(Entity<MoffRoboticsConsoleComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        var sensor = _sensors.PacketToSuitSensor(args.Data);
        if (sensor == null ||
            !ent.Comp.SensorTypes.Contains(sensor.SensorType) ||
            !_borgs.TryControlData(GetEntity(sensor.OwnerUid), out var controls))
            return;

        var data = new BorgSensorStatus(sensor, controls.Value);

        ent.Comp.Cyborgs[GetEntity(sensor.OwnerUid)] = data;
        UpdateUserInterface(ent);
    }

    private void OnOpened(Entity<MoffRoboticsConsoleComponent> ent, ref BoundUIOpenedEvent ev)
    {
        UpdateUserInterface(ent);
    }

    private void OnDisable(Entity<MoffRoboticsConsoleComponent> ent, ref RoboticsConsoleDisableMessage ev)
    {

    }

    private void OnDestroy(Entity<MoffRoboticsConsoleComponent> ent, ref RoboticsConsoleDestroyMessage ev)
    {

    }

    private void UpdateUserInterface(Entity<MoffRoboticsConsoleComponent> ent)
    {
        var state = new MoffRoboticsConsoleState(ent.Comp.Cyborgs.Values.ToList(), ent.Comp.AllowBorgControl);
        _ui.SetUiState(ent.Owner, RoboticsConsoleUiKey.Key, state);
    }
}
