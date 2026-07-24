using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.SuitSensors;
using Content.Server.Radio.EntitySystems;
using Content.Server.Silicons.Borgs;
using Content.Shared._Moffstation.Robotics.RoboticsConsole;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Lock;
using Content.Shared.Robotics;
using Robust.Server.GameObjects;

namespace Content.Server._Moffstation.Robotics.RoboticsConsole;

public sealed partial class MoffRoboticsConsoleSystem : SharedMoffRoboticsConsoleSystem
{
    [Dependency] private SuitSensorSystem _sensors = default!;
    [Dependency] private BorgSystem _borgs = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private LockSystem _lock = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private RadioSystem _radio = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        Subs.BuiEvents<MoffRoboticsConsoleComponent>(RoboticsConsoleUiKey.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnOpened);
                subs.Event<MoffRoboticsConsoleDisableMessage>(OnDisable);
                subs.Event<MoffRoboticsConsoleDestroyMessage>(OnDestroy);
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

    private void OnDisable(Entity<MoffRoboticsConsoleComponent> ent, ref MoffRoboticsConsoleDisableMessage args)
    {
        if (!ent.Comp.AllowBorgControl ||
            _lock.IsLocked(ent.Owner) ||
            !ent.Comp.Cyborgs.TryGetValue(GetEntity(args.Target), out var controls))
            return;

        var payload = new NetworkPayload()
        {
            [DeviceNetworkConstants.Command] = RoboticsConsoleConstants.NET_DISABLE_COMMAND,
        };

        if (!TryGetAddress(args.Target, out var address))
            return;

        _deviceNetwork.QueuePacket(ent, address, payload);
        _adminLogger.Add(LogType.Action,
            LogImpact.High,
            $"{ToPrettyString(args.Actor):user} disabled borg {controls.Name} with address {address}");
    }

    private void OnDestroy(Entity<MoffRoboticsConsoleComponent> ent, ref MoffRoboticsConsoleDestroyMessage args)
    {
        if (!ent.Comp.AllowBorgControl ||
            _lock.IsLocked(ent.Owner) ||
            !ent.Comp.Cyborgs.TryGetValue(GetEntity(args.Target), out var controls))
            return;

        var payload = new NetworkPayload()
        {
            [DeviceNetworkConstants.Command] = RoboticsConsoleConstants.NET_DESTROY_COMMAND,
        };

        if (!TryGetAddress(args.Target, out var address))
            return;

        _deviceNetwork.QueuePacket(ent, address, payload);

        var message = Loc.GetString(ent.Comp.DestroyMessage, ("name", controls.Name));
        _radio.SendRadioMessage(ent, message, ent.Comp.RadioChannel, ent);
        _adminLogger.Add(LogType.Action,
            LogImpact.Extreme,
            $"{ToPrettyString(args.Actor):user} destroyed borg {controls.Name} with address {address}");
    }

    private void UpdateUserInterface(Entity<MoffRoboticsConsoleComponent> ent)
    {
        var state = new MoffRoboticsConsoleState(ent.Comp.Cyborgs.Values.ToList(), ent.Comp.AllowBorgControl);
        _ui.SetUiState(ent.Owner, RoboticsConsoleUiKey.Key, state);
    }


    private bool TryGetAddress(NetEntity borg, [NotNullWhen(true)] out string? address)
    {
        address = null;
        if (!TryComp<DeviceNetworkComponent>(GetEntity(borg), out var comp))
            return false;

        address = comp.Address;
        return true;
    }

}
