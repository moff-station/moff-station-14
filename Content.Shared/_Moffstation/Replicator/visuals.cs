using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Replicator;

/// Keys for <see cref="AppearanceComponent.AppearanceData"/> to set visuals for replicators.
[Serializable, NetSerializable]
public enum ReplicatorVisuals : byte
{
    Combat,
    Queen,
}

/// Layer IDs for replicator nests.
[Serializable, NetSerializable]
public enum ReplicatorNestVisuals : byte
{
    Level1,
    Level2,
    Level3,
    Level1Unshaded,
    Level2Unshaded,
    Level3Unshaded,
}

/// Keys for <see cref="AppearanceComponent.AppearanceData"/> to set visuals for replicator nests.
[Serializable, NetSerializable]
public enum ReplicatorNestVisualsKeys : byte
{
    Key,
    KeyUnshaded,
}
