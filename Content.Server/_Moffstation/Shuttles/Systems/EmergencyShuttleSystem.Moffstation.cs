using Content.Server.Screens.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;

namespace Content.Server.Shuttles.Systems;

public sealed partial class EmergencyShuttleSystem
{
    /// <summary>
    /// Sets the station screens to count down from a Timespan.
    /// </summary>
    public void UpdateRoundEndScreens(Entity<DeviceNetworkComponent?> shuttle, TimeSpan countdown)
    {
        if (!Resolve(shuttle, ref shuttle.Comp, false))
            return;

        var payload = new NetworkPayload
        {
            [ShuttleTimerMasks.ShuttleMap] = shuttle.Owner,
            [ShuttleTimerMasks.SourceMap] = _roundEnd.GetCentcomm(),
            [ShuttleTimerMasks.DestMap] = _roundEnd.GetStation(),
            [ShuttleTimerMasks.ShuttleTime] = countdown,
            [ShuttleTimerMasks.SourceTime] = countdown,
            [ShuttleTimerMasks.DestTime] = countdown,
        };

        // by popular request
        // https://discord.com/channels/310555209753690112/770682801607278632/1189989482234126356
        if (_random.Next(1000) == 0)
        {
            payload.Add(ScreenMasks.Text, ShuttleTimerMasks.Kill);
            payload.Add(ScreenMasks.Color, Color.Red);
        }
        else
            payload.Add(ScreenMasks.Text, ShuttleTimerMasks.Bye);

        _deviceNetworkSystem.QueuePacket(shuttle, null, payload, shuttle.Comp.TransmitFrequency);
    }
}
