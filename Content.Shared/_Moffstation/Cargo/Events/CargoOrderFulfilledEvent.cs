using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Cargo.Events;

/// <summary>
/// Event raised on an entity when it's created by fulfilling a cargo order.
/// </summary>
[Serializable, NetSerializable]
public sealed class CargoOrderFulfilledEvent : EntityEventArgs;
