using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Prayers;

/// This even is raised for Admin's clients to indicate that a prayer has been sent.
[Serializable, NetSerializable]
public sealed partial class PrayerEvent : EntityEventArgs;

