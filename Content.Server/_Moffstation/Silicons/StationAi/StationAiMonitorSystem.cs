using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Shared._Moffstation.Silicons.StationAi;
using Content.Shared.Chat;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Robust.Server.Player;

namespace Content.Server._Moffstation.Silicons.StationAi;

public sealed class StationAiMonitorSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiMonitorComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    private void OnPacketReceived(EntityUid uid, StationAiMonitorComponent component, DeviceNetworkPacketEvent e)
    {
        var payload =  e.Data;

        if (!payload.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;

        if (command != DeviceNetworkConstants.CmdUpdatedState)
            return;

        if (!payload.TryGetValue(SuitSensorConstants.NET_STATUS_COLLECTION,
                out Dictionary<string, SuitSensorStatus>? sensorStatus))
            return;

        component.ConnectedSensors = sensorStatus;
        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, StationAiMonitorComponent? component = null)
    {
        if (! Resolve(uid, ref component))
            return;

        if (!_uiSystem.IsUiOpen(uid, StationAiMonitorUIKey.Key))
            return;

        var xform = Transform(uid);

        if (xform.GridUid != null)
            EnsureComp<NavMapComponent>(xform.GridUid.Value);

        var allSensors = component.ConnectedSensors.Values.ToList();
        _uiSystem.SetUiState(uid, StationAiMonitorUIKey.Key, new CrewMonitoringState(allSensors));
    }

}
