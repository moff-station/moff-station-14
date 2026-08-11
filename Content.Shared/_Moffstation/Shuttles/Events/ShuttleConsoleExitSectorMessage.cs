using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Shuttles.Events;

/// <summary>
/// Sent when a shuttle console's exit sector button is confirmed.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShuttleConsoleExitSectorMessage : BoundUserInterfaceMessage;
