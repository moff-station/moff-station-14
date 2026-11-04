namespace Content.Server.Shuttles.Events;

/// <summary>
/// An event raised on the Emergency shuttle when it arrives at the station
/// </summary>
[ByRefEvent]
public record struct EmergencyShuttleArrivedEvent;
