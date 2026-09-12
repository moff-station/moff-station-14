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
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Robotics.RoboticsConsole;

public sealed partial class MoffRoboticsConsoleSystem : EntitySystem
{
    [Dependency] private IGameTiming _time = default!;
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _time.CurTime;
        var query = EntityQueryEnumerator<MoffRoboticsConsoleComponent>();

        HashSet<EntityUid> removed = [];
        while (query.MoveNext(out var uid, out var comp))
        {
            // remove cyborgs that have not pinged for a long time
            foreach (var (borg, data) in comp.Cyborgs)
            {
                if (now >= data.Timeout)
                    removed.Add(borg);
            }

            // needed to prevent modifying while iterating it
            foreach (var borg in removed)
            {
                comp.Cyborgs.Remove(borg);
            }

            if (removed.Count > 0)
                UpdateUserInterface((uid, comp));

            removed.Clear();
        }
    }

    [SubscribeLocalEvent]
    private void OnPacketReceived(Entity<MoffRoboticsConsoleComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (_sensors.PacketToSuitSensor(args.Data) is not {} sensor ||
            !ent.Comp.SensorTypes.Contains(sensor.SensorType) ||
            _borgs.ControlDataOrNull(GetEntity(sensor.OwnerUid)) is not {} controls)
            return;

        var data = new BorgSensorStatus(sensor, controls, _time.CurTime + ent.Comp.Timeout);

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

        if (AddressOrNull(args.Target) is not {} address)
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

        if (AddressOrNull(args.Target) is not {} address)
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


    private string? AddressOrNull(NetEntity borg) => CompOrNull<DeviceNetworkComponent>(GetEntity(borg))?.Address;
}
